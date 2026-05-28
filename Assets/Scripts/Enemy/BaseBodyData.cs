using NaughtyAttributes;
using System;
using UnityEngine;

namespace Base.BaseBodyData
{ 
    [Serializable]
    public class BaseBodyData
    {
        [Label("基礎移動速度")]
        [SerializeField]
        [AllowNesting]
        public float basicMoveSpeed;      //移動速度
        [Label("基礎攻擊力")]
        [SerializeField]
        [AllowNesting]
        public float basicATK;            //攻擊力
        [Label("基礎防禦力")]
        [SerializeField]
        [AllowNesting]
        public float basicDEF;            //防禦力
        [Label("基礎血量")]
        [SerializeField]
        [AllowNesting]
        public float basicHp;             //基礎血量
        [Label("基礎魔力")]
        [SerializeField]
        [AllowNesting]
        public float basicMp;             //基礎魔力
        [Label("攻擊頻率")]
        [SerializeField]
        [AllowNesting]
        public float atkSpeed;
    }
}
