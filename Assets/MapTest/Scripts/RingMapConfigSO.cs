using System;
using System.Collections.Generic;
using ShowX.Utils;
using UnityEditor;
using UnityEngine;

namespace MapTest
{
    /// <summary>
    /// 单层环形地图配置：仅定义本层 cell 数量（满环 8）。
    /// 层间错开、层大小、层方向与 cell 显示均由全局参数自动计算或来自 RingLayer 预制体，不在此逐层设置。
    /// </summary>
    [Serializable]
    public struct RingLayerConfig
    {
        // ==================== 字段 ====================

        /// <summary>
        /// 本层 cell 数量，运行时会钳制到 [0, 8]。
        /// </summary>
        [SerializeField] private int _count;

        // ==================== 属性 ====================

        /// <summary>本层 cell 数量。</summary>
        public int Count { get => _count; set => _count = value; }
    }

    /// <summary>
    /// 多层环形地图配置：按层列出每层 cell 数量，携带层间错开范围、等比缩放模型与滚轮步长参数。
    /// cell 默认尺寸与双态贴图配置于 RingLayer 预制体内，不在此配置（避免配置冗余与显示漂移）。
    /// 供 <see cref="RingMapGenerator"/> 加载后生成整图。
    /// </summary>
    [CreateAssetMenu(menuName = "Ring Map Config")]
    public class RingMapConfigSO : ScriptableObject
    {
        // ==================== 常量 ====================

        /// <summary>满环槽位设计上限（默认值）。</summary>
        private const int DEFAULT_MAX_SLOTS = 8;

        /// <summary>整圆角度（度）。</summary>
        private const float FULL_CIRCLE_ANGLE = 360f;

        /// <summary>层间浮动幅度默认值（单位：cell）。</summary>
        private const float DEFAULT_STAGGER_JITTER_CELLS = 2f;

        // ==================== 多层配置 ====================

        /// <summary>
        /// 每层配置，数组顺序即层 index（0 为最上层/首层）。
        /// </summary>
        [Header("多层配置")]
        [SerializeField] private List<RingLayerConfig> _layers = new List<RingLayerConfig>();

        /// <summary>
        /// RingLayer 预制体（内含 UiRingLayout），cell 预制体与双态贴图配置于 RingConfigSO，生成器只实例化不拼装 cell。
        /// </summary>
        [Header("层基础配置")]
        [SerializeField] private RingLayer _layerPrefab;

        /// <summary>
        /// 满环槽位数量（angleStep = 360 / maxSlots），由生成器计算 cell 角度并下传至 RingLayer（原 RingConfigSO 字段迁入）。
        /// </summary>
        [SerializeField] private int _maxSlots = DEFAULT_MAX_SLOTS;

        /// <summary>
        /// 层排列方向：true=顺时针，false=逆时针（原 RingConfigSO 字段迁入，经 RingLayer 下传至 UiRingLayout）。
        /// </summary>
        [SerializeField] private bool _isClockwise = true;

        /// <summary>
        /// 第一层起始角随机范围 [min, max]（无上一层，直接在整圆内随机取初始角度）。
        /// </summary>
        [Header("角随机范围")]
        [SerializeField] private Vector2 _firstLayerStaggerRange = new Vector2(0f, FULL_CIRCLE_ANGLE);

        /// <summary>
        /// 后续层在"完全错开基准角"上的左右浮动幅度（单位：cell，1 cell = 360 / maxSlots，可填 1 / 1.5 / 2 等）。
        /// </summary>
        [SerializeField] private float _staggerJitterCells = DEFAULT_STAGGER_JITTER_CELLS;

        /// <summary>
        /// 等比缩放比例（&lt;1）：层 i 基础 scale = ratio^i；offset 每 +1，全体层等比放大一级。
        /// </summary>
        [Header("等比缩放模型")]
        [SerializeField] private float _scaleRatio = 0.6f;

        /// <summary>
        /// 已停用：缩放 offset 上限（保留字段兼容旧序列化数据）。
        /// 浏览范围现由 RingMapGenerator 按 [激活层, 末层] 钳制，不再读取此值。
        /// </summary>
        [SerializeField] private float _maxScaleOffset = 4f;

        /// <summary>
        /// 每格滚轮推进的 offset 步长（1 = 整层递进一级）。
        /// </summary>
        [SerializeField] private float _wheelStep = 1f;

        /// <summary>
        /// 每格滚轮拨动推进 offset 的分数单位数量：滚满此格数 = WheelStep 一整层（默认 5 格）。
        /// </summary>
        [SerializeField] private int _wheelTicksPerLevel = 5;

        /// <summary>
        /// offset 平滑渐变速度（层/秒）：滚轮或左右键触发后，显示值以此速度向目标值逼近。
        /// </summary>
        [SerializeField] private float _wheelTweenSpeed = 2f;

        // ==================== 属性 ====================

        /// <summary>每层配置列表。</summary>
        public List<RingLayerConfig> Layers => _layers;

        /// <summary>RingLayer 预制体。</summary>
        public RingLayer LayerPrefab => _layerPrefab;

        /// <summary>满环槽位数量（angleStep = 360 / maxSlots）。</summary>
        public int MaxSlots => _maxSlots;

        /// <summary>层排列方向（true=顺时针）。</summary>
        public bool IsClockwise => _isClockwise;

        /// <summary>第一层起始角随机范围。</summary>
        public Vector2 FirstLayerStaggerRange => _firstLayerStaggerRange;

        /// <summary>后续层浮动幅度（单位：cell 角度）。</summary>
        public float StaggerJitterCells => _staggerJitterCells;

        /// <summary>等比缩放比例。</summary>
        public float ScaleRatio => _scaleRatio;

        /// <summary>已停用：缩放 offset 上限（保留兼容，不再被引用）。</summary>
        public float MaxScaleOffset => _maxScaleOffset;

        /// <summary>每格滚轮推进的 offset 步长。</summary>
        public float WheelStep => _wheelStep;

        /// <summary>每格滚轮推进 offset 的分数单位数量（滚满此格数 = WheelStep 一整层）。</summary>
        public int WheelTicksPerLevel => _wheelTicksPerLevel;

        /// <summary>offset 平滑渐变速度（层/秒）。</summary>
        public float WheelTweenSpeed => _wheelTweenSpeed;

        // ==================== 编辑器工具 ====================

#if UNITY_EDITOR
        /// <summary>
        /// 将当前配置标记为脏并保存资产文件（编辑器存盘入口）。
        /// </summary>
        [Button("保存资产")]
        private void saveAsset()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            XLogger.LogInfo("RingMapConfigSO", "saveAsset: 配置已保存");
        }
#endif
    }
}
