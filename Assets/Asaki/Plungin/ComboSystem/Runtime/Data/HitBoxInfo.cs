using UnityEngine;

namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 判定框信息 - 通过回调传递给外部系统
    /// </summary>
    public struct HitBoxInfo
    {
        public string HitBoxId;
        public Collider Collider; // 激活的Collider，外部可以用它做检测
        public GameObject Owner; // 攻击者
        public ComboMove MoveData; // 招式数据（外部可读取伤害等信息）
    }
}
