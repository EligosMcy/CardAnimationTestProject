using ShowX.Utils;
using UnityEngine;

namespace MapTest
{
    /// <summary>
    /// 圆环几何计算器：只读取 <see cref="RingConfigSO"/>，按索引输出每个 cell 的位置与旋转。
    /// 不实例化 cell、不做生命周期管理、不驱动布局；实际调用方为 RingLayer。
    /// </summary>
    public class UiRingLayout : MonoBehaviour
    {
        // ==================== 常量 ====================

        /// <summary>整圆角度（度）。</summary>
        private const float FULL_CIRCLE_ANGLE = 360f;

        /// <summary>角度方向：顺时针为 1。</summary>
        private const int CLOCKWISE_DIRECTION = 1;

        /// <summary>角度方向：逆时针为 -1。</summary>
        private const int COUNTER_CLOCKWISE_DIRECTION = -1;

        /// <summary>满环槽位数量默认值（RingLayer 注入前兜底）。</summary>
        private const int DEFAULT_MAX_SLOTS = 8;

        // ==================== 配置 ====================

        /// <summary>
        /// 圆环配置文件（由 RingLayer 注入），提供半径与起始角等布局参数。
        /// </summary>
        [SerializeField] private RingConfigSO _config;

        /// <summary>
        /// 满环槽位数量（由 RingLayer 注入，angleStep = 360 / maxSlots）。
        /// </summary>
        private int _maxSlots = DEFAULT_MAX_SLOTS;

        /// <summary>
        /// 是否顺时针排列（由 RingLayer 注入）。
        /// </summary>
        private bool _isClockwise = true;

        // ==================== 几何查询 ====================

        /// <summary>
        /// 计算第 index 个 cell 的布局数据：位置（anchoredPosition）与旋转（localEulerAngles.z）。
        /// 槽位 = index mod maxSlots，角度从 12 点起算沿 direction 方向走，extraAngleOffset 叠加层间错开。
        /// </summary>
        public CellPlacement GetCellPlacement(int index, float extraAngleOffset)
        {
            if (_config == null)
            {
                XLogger.LogError("UiRingLayout", "GetCellPlacement: _config 为空");
                return new CellPlacement();
            }
            int maxSlots = Mathf.Max(1, _maxSlots);
            int slot = index % maxSlots;
            float angleStep = FULL_CIRCLE_ANGLE / maxSlots;
            int direction = _isClockwise ? CLOCKWISE_DIRECTION : COUNTER_CLOCKWISE_DIRECTION;
            float angleDeg = _config.StartAngleOffset + extraAngleOffset + direction * slot * angleStep;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            CellPlacement placement = new CellPlacement
            {
                Position = new Vector2(_config.Radius * Mathf.Sin(angleRad), _config.Radius * Mathf.Cos(angleRad)),
                AngleDeg = -angleDeg,
            };
            return placement;
        }

        // ==================== 属性 ====================

        /// <summary>圆环配置文件。</summary>
        public RingConfigSO Config { get => _config; set => _config = value; }

        /// <summary>满环槽位数量（由 RingLayer 注入）。</summary>
        public int MaxSlots { get => _maxSlots; set => _maxSlots = value; }

        /// <summary>是否顺时针排列（由 RingLayer 注入）。</summary>
        public bool IsClockwise { get => _isClockwise; set => _isClockwise = value; }
    }

    /// <summary>
    /// cell 布局数据：位置（RectTransform.anchoredPosition）与旋转角度（localEulerAngles.z）。
    /// </summary>
    public struct CellPlacement
    {
        /// <summary>位置。</summary>
        public Vector2 Position;

        /// <summary>旋转角度（度）。</summary>
        public float AngleDeg;
    }
}
