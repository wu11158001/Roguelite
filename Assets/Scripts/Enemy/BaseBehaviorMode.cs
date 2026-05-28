using NaughtyAttributes;
using System;
using UnityEngine;

namespace Base.BehaviorMode
{
    [Serializable]
    public class BaseBehaviorMode
    {
        [Label("移動模式")]
        [AllowNesting]
        public MOVE_ACTION moveType = MOVE_ACTION.FOLLOW;
        [Label("出界處理")]
        [AllowNesting]
        public OUTBOUNDS_ACTION outboundsAction = OUTBOUNDS_ACTION.RE_ENTER;
        [Label("是否移動")]
        [AllowNesting]
        public bool isMove; //是否移動
    }
}
