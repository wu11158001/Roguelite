using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// 地圖道具_無敵
/// </summary>
public class MapProps_Invincible : BaseMapProps
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("MapProps_Invincible")]
    [Label("無敵時間")]
    [SerializeField] private float _invincibleTime = 5;

    public override void OnPickUpDo()
    {
        GameplayManager.CurrentContext.GameController.SetCharacterInvincible(_invincibleTime).Forget();

        Transform target = GameplayManager.CurrentContext.ControlCharacter.BottomPoint;
        SpawnInvincibleEffect(target, _invincibleTime);
    }

    /// <summary>
    /// 產生無敵效果
    /// </summary>
    /// <param name="target"></param>
    private void SpawnInvincibleEffect(Transform target, float recycleTime)
    {
        EffectData data = GameStateData.AllEffectPrefabData.GetEffect(EFFET_TYPE.Invincible);
        if (data != null)
        {
            GameplayManager.CurrentContext.GameScenePool.SpawnObject(
                parentName: "無敵效果",
                assetRef: data.PrefabReference,
                position: target.position,
                rotation: target.rotation,
                callback: (obj) =>
                {
                    obj.transform.SetParent(target);
                    obj.transform.position = target.position;

                    if (obj.TryGetComponent(out EffectRecycle effectRecycle))
                    {
                        effectRecycle.Setup(data.PrefabReference, recycleTime);
                    }
                });
        }
    }
}
