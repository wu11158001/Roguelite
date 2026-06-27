using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using System.Collections;
using UniRx;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UniRx.Triggers;

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

        BindViewModel();

        // 開始自動產生敵人
        GameplayManager.CurrentContext.EnemySystemManager.InitAndStartAutoSpawn(this);
    }

    private void BindViewModel()
    {
        // 每幀驅動
        this.UpdateAsObservable()
        .Where(_ => !GameplayManager.CurrentContext.GameController.IsGamePause &&
                    !GameplayManager.CurrentContext.GameController.IsGameOver)
        .Subscribe(_ =>
        {
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
        })
        .AddTo(this);
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
                    PlayHitAnim();
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

        ViewManager.Instance?.ClearAll();
        ViewManager.Instance.OpenView<GameOverView>(
            viewType: VIEW_TYPE.GameOverView, 
            callback: (view) =>
            {
                GameplayManager.CurrentContext.GameController.GameOverClear();
            }).Forget();
    }

    [Label("距離測試")] [SerializeField] private float _distanceRadius = 30;
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _distanceRadius);
    }
}
