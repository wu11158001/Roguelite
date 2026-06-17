using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using System.Collections;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Linq;

/// <summary>
/// 玩家角色
/// </summary>
public class PlayerView : BaseCharacter
{
    public Vector2 InputVector { get; set; }
    private Vector2 _joystickInputVector;

    // 動畫
    private readonly int _isMovingParamId = Animator.StringToHash("IsMove");
    private readonly int _dieParamId = Animator.StringToHash("Die");

    protected SphereCollider _pickRange;

    private HpBarView _hpBarView;
    private CharacterConfigData _characterConfig;

    private Coroutine _hitCoroutine;
    private PlayerController _controller;

    public override void OnDestroy()
    {
        StopAllCoroutines();
        _controller?.Dispose();

        base.OnDestroy();
    }

    public override void Remove()
    {
        _controller?.Deactivate();
        base.Remove();
    }

    protected override void Awake()
    {
        base.Awake();

        _pickRange = transform.Find("CharacterNecessary/PickupRange").GetComponent<SphereCollider>();
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        _controller ??= new PlayerController(this);

        GameplayManager.CurrentContext.ControlCharacter = this;
        _characterConfig = GameStateData.SelectedCharacter;

        // 攝影機與初始技能設定
        SetupCameraAndInitSkill();

        // 產生生命條與注入事件
        SpawnHpBarAndBind();

        _controller.Activate();

        // 開始自動產生敵人
        GameplayManager.CurrentContext.EnemySystemManager.InitAndStartAutoSpawn(this);
    }

    private void Update()
    {
        if (GameplayManager.CurrentContext.GameController.IsGamePause ||
            GameplayManager.CurrentContext.GameController.IsGameOver) return;

        // 移動控制
        Vector2 keyboardInput = GetKeyboardInput();
        // 複合輸入處理：比較鍵盤與搖桿，誰的推力大就用誰
        if (keyboardInput.sqrMagnitude > _joystickInputVector.sqrMagnitude)
        {
            InputVector = keyboardInput;
        }
        else
        {
            InputVector = _joystickInputVector;
        }
        // 執行移動
        _controller.ExecuteTick(InputVector, Time.deltaTime);

        // 測試用鍵盤輸入監聽
        TestDebugInput();
    }

    /// <summary>
    /// 鍵盤輸入控制移動
    /// </summary>
    /// <returns></returns>
    private Vector2 GetKeyboardInput()
    {
        if (Keyboard.current == null) return Vector2.zero;

        float x = 0;
        float y = 0;

        // WASD
        if (Keyboard.current.wKey.isPressed) y += 1f;
        if (Keyboard.current.sKey.isPressed) y -= 1f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;

        // 方向鍵
        if (Keyboard.current.upArrowKey.isPressed) y += 1f;
        if (Keyboard.current.downArrowKey.isPressed) y -= 1f;
        if (Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.rightArrowKey.isPressed) x += 1f;

        // 回傳正規化後的向量(避免斜對角移動變快)
        return new Vector2(x, y).normalized;
    }

    /// <summary>
    /// 更新移動
    /// </summary>
    /// <param name="translation"></param>
    /// <param name="rotation"></param>
    public void UpdateMovement(Vector3 translation, Quaternion rotation)
    {
        transform.Translate(translation, Space.World);
        transform.rotation = rotation;
    }

    /// <summary>
    /// 設置阻擋力
    /// </summary>
    /// <param name="blockForce"></param>
    public void SetBlockForce(Vector3 blockForce)
    {
        _controller.CurrentBlockForce = blockForce;
    }

    /// <summary>
    /// 更新動畫
    /// </summary>
    /// <param name="isMoving"></param>
    public void UpdateAnimation(bool isMoving)
    {
        Anim.SetBool(_isMovingParamId, isMoving);
    }

    /// <summary>
    /// 產生生命條與注入事件
    /// </summary>
    private void SpawnHpBarAndBind()
    {
        GameInfoUIManager gameInfoUIManager = GameplayManager.CurrentContext.GameInfoUIManager;
        if (gameInfoUIManager != null)
        {
            gameInfoUIManager.SpawnHpBar(
                target: HeadPoint,
                offset: new Vector3(0, _characterConfig.HpBarHight, 0),
                callback: (hpBar) =>
                {
                    _hpBarView = hpBar;
                }).Forget();
        }

        // 最大生命質變化
        _characterConfig.MaxHp.Subscribe(value =>
        {
            // 更新血條 UI
            float hpRatio = (float)_characterConfig.Hp.Value / value;
            if (_hpBarView != null) _hpBarView.SetHpBar(hpRatio);
        }).AddTo(this);

        // 監聽角色生命變化
        _characterConfig.Hp.Pairwise()
            .Subscribe(pair =>
            {
                int previousHp = pair.Previous;
                int currentHp = pair.Current;

                if (currentHp > previousHp)
                {
                    // 回復生命
                    _controller.TriggerHpRecoverEffect();
                }
                // 角色無敵
                else if (GameplayManager.CurrentContext.GameController.IsCharacterInvincible)
                {
                    _characterConfig.Hp.Value = previousHp;
                }
                else if (previousHp > currentHp)
                {
                    // 減少生命
                    if (_hitCoroutine != null) StopCoroutine(_hitCoroutine);
                    _hitCoroutine = StartCoroutine(IGetHitAnim());
                }

                // 更新血條 UI
                float hpRatio = (float)currentHp / _characterConfig.MaxHp.Value;
                if (_hpBarView != null) _hpBarView.SetHpBar(hpRatio);

                // 死亡
                if (currentHp <= 0)
                {
                    OnDie();
                    return;
                }
            })
            .AddTo(this);

        // 監聽拾取範圍
        _characterConfig.PickupRange.Subscribe((value) => _pickRange.radius = value).AddTo(this);

        // 監聽時間
        GameplayManager.CurrentContext.GameController.ElapsedTime.Subscribe((t) =>
        {
            // 已達到關卡限制時間
            int timeLimit = GameStateData.SelectLevel.TimeLimit;
            if(timeLimit - t <= 0 && !GameplayManager.CurrentContext.GameController.IsGameOver)
            {
                // 紀錄通關資料
                PlayerInfoData infoData = PlayerInfoStateData.PlayerInfo.Value;
                infoData.PassLevel = GameStateData.SelectLevel.LevelIndex + 1;
                PlayerInfoStateData.PlayerInfo.Value = infoData;

                // 角色死亡
                _characterConfig.Hp.Value = 0;
            }

        }).AddTo(this);

        // 監聽搖桿輸入
        GameStateData.JoystickInput
            .Subscribe(joystickValue =>
            {
                _joystickInputVector = joystickValue;
            })
            .AddTo(this);
    }

    /// <summary>
    /// 攝影機與初始技能設定
    /// </summary>
    private void SetupCameraAndInitSkill()
    {
        GameCamera gameCameraView = GameObject.FindFirstObjectByType<GameCamera>();
        if (gameCameraView != null) gameCameraView.Setup(transform);

        SkillItemData skillItemData = GameStateData.AllSkillConfigData.GetActiveSkill(_characterConfig.InitSkill, 1);
        GameplayManager.CurrentContext.SkillController.AddOrUpgradeSkill(newSkill: skillItemData);
    }

    /// <summary>
    /// 死亡
    /// </summary>
    private void OnDie()
    {
        GameplayManager.CurrentContext.GameController.SetGameOver();

        Anim.SetTrigger(_dieParamId);
        _hpBarView.gameObject.SetActive(false);

        StartCoroutine(IHandleDieRoutine());
    }

    /// <summary>
    /// 處理死亡流程
    /// </summary>
    /// <returns></returns>
    private IEnumerator IHandleDieRoutine()
    {
        // 等待動畫切換
        while (Anim.IsInTransition(0) || !Anim.GetCurrentAnimatorStateInfo(0).IsName("Die"))
        {
            yield return null;
        }

        // 取得動畫的實際長度
        AnimatorStateInfo stateInfo = Anim.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;
        yield return new WaitForSeconds(animationLength);

        GameplayManager.CurrentContext.GameController.GameOverClear();
        ViewManager.Instance.OpenView<GameOverView>(viewType: VIEW_TYPE.GameOverView).Forget();
    }

    #region 測試用

    /// <summary>
    /// 測試用
    /// </summary>
    private void TestDebugInput()
    {
        if (Keyboard.current.numpad1Key.wasPressedThisFrame) _controller.GainExp(50);
        if (Keyboard.current.numpad4Key.wasPressedThisFrame) GameplayManager.CurrentContext.CharacterController.OnPlayerGetHit(20);
        if (Keyboard.current.numpad5Key.wasPressedThisFrame) GameplayManager.CurrentContext.CharacterController.OnPlayerHpRecover(1000);

        if (Keyboard.current.numpad9Key.wasPressedThisFrame)
        {
            GameplayManager.CurrentContext.GameController.GamePause(true);
            ViewManager.Instance.OpenView<BossBonusView>(viewType: VIEW_TYPE.BossBonusView).Forget();
        }

        // 直升技能:追蹤
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            Test_GainSkill(GameStateData.AllSkillConfigData.GetActiveSkill(SKILL_TYPE.Skill_Tracking, 1));
        }
        // 直升技能:靈氣
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            Test_GainSkill(GameStateData.AllSkillConfigData.GetActiveSkill(SKILL_TYPE.Skill_Aura, 1));
        }
        // 直升技能:圍繞
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            Test_GainSkill(GameStateData.AllSkillConfigData.GetActiveSkill(SKILL_TYPE.Skill_Around, 1));
        }
        // 直升技能:前方打擊
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            Test_GainSkill(GameStateData.AllSkillConfigData.GetActiveSkill(SKILL_TYPE.Skill_FrontHit, 1));
        }
        // 直升技能:範圍減速
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            Test_GainSkill(GameStateData.AllSkillConfigData.GetActiveSkill(SKILL_TYPE.Skill_RangeSlow, 1));
        }
        // 直升技能:單體攻擊
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
        {
            Test_GainSkill(GameStateData.AllSkillConfigData.GetActiveSkill(SKILL_TYPE.Skill_SingleHit, 1));
        }
        // 直升技能:飛鏢
        if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            Test_GainSkill(GameStateData.AllSkillConfigData.GetActiveSkill(SKILL_TYPE.Skill_StraightProjectile, 1));
        }
        // 直升技能:機器人
        if (Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            Test_GainSkill(GameStateData.AllSkillConfigData.GetActiveSkill(SKILL_TYPE.Skill_WaterSplash, 1));
        }

        // 直升技能:被動
        if (Keyboard.current.digit9Key.wasPressedThisFrame)
        {
            Test_GainSkill(GameStateData.AllSkillConfigData.GetPassiveSkill(PASSIVE_SKILL_TYPE.EffectRange, 1));
        }
    }

    /// <summary>
    /// 測試用:獲取技能
    /// </summary>
    /// <param name="skillItemData"></param>
    private void Test_GainSkill(SkillItemData skillItemData)
    {
        if (skillItemData != null)
        {
            SkillItemData targetSkill = Test_GetNextLevelSkill(skillItemData);
            if (targetSkill != null)
            {
                Debug.Log($"成功觸發技能獲取/升級！技能：{targetSkill.SkillType}, 等級：{targetSkill.SkillLevel}");
                GameplayManager.CurrentContext.SkillController.AddOrUpgradeSkill(targetSkill);
            }
        }
    }

    /// <summary>
    /// 測試用:獲取下一級技能
    /// </summary>
    /// <param name="skillItemData"></param>
    /// <returns></returns>
    private SkillItemData Test_GetNextLevelSkill(SkillItemData skillItemData)
    {
        var allConfigs = GameStateData.AllSkillConfigData.AllSkillItemConfigs.SelectMany(c => c.SkillItems).ToList();
        var ownedSkills = GameplayManager.CurrentContext.SkillController.OwnSkills.ToList();
        var activeOwned = ownedSkills.Where(s => !s.IsPassive && !s.IsProps).ToList();

        if (skillItemData.IsProps) return null;

        // 檢查目前是否已經擁有「同種類」的技能
        bool alreadyHasSkill = skillItemData.IsPassive
            ? ownedSkills.Any(o => o.IsPassive && o.PassiveType == skillItemData.PassiveType)
            : ownedSkills.Any(o => !o.IsPassive && o.SkillType == skillItemData.SkillType);

        if (!alreadyHasSkill && !skillItemData.IsPassive && activeOwned.Count >= 6)
        {
            Debug.LogWarning("主動技能已滿 6 個，無法獲得新主動技能！");
            return null;
        }

        // 如果是全新獲得的技能 (當前等級為 1 且玩家沒有)
        if (!alreadyHasSkill)
        {
            // 確保設定檔裡存在這筆等級 1 的資料
            return allConfigs.FirstOrDefault(s =>
                s.IsPassive == skillItemData.IsPassive &&
                !s.IsProps &&
                (skillItemData.IsPassive ? s.PassiveType == skillItemData.PassiveType : s.SkillType == skillItemData.SkillType) &&
                s.SkillLevel == 1);
        }

        // 已經有了，查找下一級
        var currentOwnedSkill = ownedSkills.FirstOrDefault(o =>
            o.IsPassive == skillItemData.IsPassive &&
            (skillItemData.IsPassive ? o.PassiveType == skillItemData.PassiveType : o.SkillType == skillItemData.SkillType));

        int currentLevel = currentOwnedSkill != null ? currentOwnedSkill.SkillLevel : skillItemData.SkillLevel;

        SkillItemData nextLevel = allConfigs.FirstOrDefault(s =>
            s.IsPassive == skillItemData.IsPassive &&
            !s.IsProps &&
            (skillItemData.IsPassive ? s.PassiveType == skillItemData.PassiveType : s.SkillType == skillItemData.SkillType) &&
            s.SkillLevel == currentLevel + 1);

        if (nextLevel == null)
        {
            Debug.LogWarning($"該技能已達到最大等級 (等級 {currentLevel})，無法再升級！");
            return null;
        }

        return nextLevel;
    }


    [SerializeField] private float distanceRadius = 30;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanceRadius);
    }

    #endregion
}
