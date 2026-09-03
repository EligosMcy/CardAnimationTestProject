using UnityEngine;

namespace SpineTest.Battle
{
    /// <summary>
    /// 战斗演出参数配置资产:单一配置源,收纳 SpineBattleDirector 与 BattleStage 的
    /// 全部数值/布尔/名称类可调参数;默认值与当前演示场景数据一致,只需修改本资产文件
    /// 即可整体调整演出效果。场景对象引用(演出对/锚点/相机/幕布等)不放入本资产。
    /// </summary>
    [CreateAssetMenu(fileName = "SpineBattleCinematicSettings", menuName = "SpineTest/Battle/SpineBattleCinematicSettings")]
    public class SpineBattleSettings : ScriptableObject
    {
        [Header("节拍参数(秒,以攻击方攻击 clip 时间为基准)")]
        [Tooltip("AttackStart 触发点:前摇结束 / 扑击起点,到达该 clip 时间触发打击节拍")]
        public float AttackStartTime = 0.7f;

        [Tooltip("打击节拍时攻击方跳转并定格的攻击 clip 帧(扑击到位 / 命中帧)")]
        public float AttackFreezeFrame = 0.75f;

        [Tooltip("受击方 Hit clip 内被定格采样的受击帧")]
        public float HitFrameTime = 0.3f;

        [Header("表现窗口与回位(秒)")]
        [Tooltip("表现窗口总时长(定格扩大 + 相机晃动),到时触发恢复节拍")]
        public float PresentationDuration = 0.6f;

        [Tooltip("缩小回 Home 原位的时长(回位完成后才续播)")]
        public float ReturnDuration = 0.15f;

        [Header("缩放")]
        [Tooltip("演出对挂载后的放大倍率(乘性,保留镜像负 X 缩放)")]
        public float FocusScaleMultiplier = 2f;

        [Tooltip("SpiderGroup/background 的轻微放大倍率,须小于 FocusScaleMultiplier")]
        public float GroupScaleMultiplier = 1.1f;

        [Header("相机晃动(表现窗口内冲击表现)")]
        [Tooltip("相机位置晃动幅度(世界单位,以演出前相机基准为中心)")]
        public float CameraShakeAmplitude = 0.05f;

        [Tooltip("相机晃动噪声速度/频率因子")]
        public float CameraShakeSpeed = 25f;

        [Tooltip("相机旋转晃动幅度(角度,预留;0 表示不旋转)")]
        public float CameraShakeRotationAmplitude = 0f;

        [Header("动画与调试")]
        [Tooltip("攻击方的攻击动画 clip 名")]
        public string AttackClipName = "Attack1";

        [Tooltip("启用后按 T 键打印当前演出对双方动画时间,便于人工精调节拍参数")]
        public bool EnableDebugKeys = true;
    }
}
