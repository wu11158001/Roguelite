using UnityEngine;
using UniRx;
using System;
using UniRx.Triggers;

/// <summary>
/// 技能_水領域(畫面反彈)
/// </summary>
public class Skill_WaterAreaController : IDisposable
{
    private Skill_WaterAreaView _view;
    private Skill_WaterAreaModel _model;

    // 用來防止連續反彈的計時器
    private float _lastReflectTime = 0f;

    private readonly CompositeDisposable _disposables = new();

    public void Dispose()
    {
        _disposables?.Dispose();
    }

    public Skill_WaterAreaController(Skill_WaterAreaView view, Skill_WaterAreaModel model)
    {
        _view = view;
        _model = model;

        BindViewModel();
    }

    private void BindViewModel()
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        // 監聽範圍增加
        characterConfig.AddEffectRange
            .Subscribe(addRange =>
            {
                float totalScale = _model.SkillData.SkillEffectRange + (_model.SkillData.SkillEffectRange * addRange);
                _view.UpdateEffectRange(totalScale);
            })
            .AddTo(_disposables);

        // 每幀驅動
        _view.UpdateAsObservable()
            .Subscribe(_ =>
            {
                // 移動物件
                _view.Move(_model.MoveDirection.Value, _model.SkillData.SkillFlightSpeed);
                // 邊界偵測
                CheckBoundsAndReflect();
            })
            .AddTo(_disposables);

        // 移除監聽
        _view.OnDestroyAsObservable()
            .Subscribe(_ => Dispose())
            .AddTo(_disposables);
    }

    /// <summary>
    /// 擊中敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    /// <param name="hitData"></param>
    /// <param name="audioType"></param>
    public void HitEnemy(GameObject enemyObj, HitData hitData, AUDIO_TYPE audioType)
    {
        if (hitData == null) return;

        // 音效
        AudioManager.Instance.PlaySFX(audioType).Forget();

        // 攻擊敵人
        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);

        // 技能追蹤傷害
        GameplayManager.CurrentContext.SkillController.UpdateTrackDamageData(hitData.SkillType, hitData.Attack);
    }

    /// <summary>
    /// 檢測邊界與反彈
    /// </summary>
    private void CheckBoundsAndReflect()
    {
        // 防止在同一瞬間因為卡牆連續觸發反彈
        if (Time.time - _lastReflectTime < 0.1f) return;

        Vector3 worldPos = _view.transform.position;
        float radius = _view.CurrentWorldRadius;

        // 計算「螢幕中心點」對應在世界座標的基準深度
        float currentZ = worldPos.z;
        float currentY = worldPos.y;

        // 2. 精準算出螢幕邊界對應到世界座標的值
        Vector3 screenLeftWorld = _model.MainCamera.ScreenToWorldPoint(new Vector3(0, Screen.height / 2f, _model.MainCamera.transform.position.y - currentY));
        Vector3 screenRightWorld = _model.MainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2f, _model.MainCamera.transform.position.y - currentY));
        Vector3 screenBottomWorld = _model.MainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2f, 0, _model.MainCamera.transform.position.y - currentY));
        Vector3 screenTopWorld = _model.MainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height, _model.MainCamera.transform.position.y - currentY));

        // 實際的安全世界座標邊界
        float minX = screenLeftWorld.x + radius;
        float maxX = screenRightWorld.x - radius;
        float minZ = screenBottomWorld.z + radius;
        float maxZ = screenTopWorld.z - radius;

        bool reflected = false;
        Vector3 targetPos = _view.transform.position;

        // 3. 檢查邊界、反彈並「強行鎖定位置（Clamp）」
        if (worldPos.x <= minX)
        {
            _model.ReflectDirection(Vector3.right);
            targetPos.x = minX; // 精準卡回邊界，不使用 += 0.05f
            reflected = true;
        }
        else if (worldPos.x >= maxX)
        {
            _model.ReflectDirection(Vector3.left);
            targetPos.x = maxX;
            reflected = true;
        }

        if (worldPos.z <= minZ)
        {
            _model.ReflectDirection(Vector3.forward);
            targetPos.z = minZ;
            reflected = true;
        }
        else if (worldPos.z >= maxZ)
        {
            _model.ReflectDirection(Vector3.back);
            targetPos.z = maxZ;
            reflected = true;
        }

        // 如果有發生反彈，更新位置並記錄時間
        if (reflected)
        {
            _view.transform.position = targetPos;
            _lastReflectTime = Time.time;
        }
    }
}
