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
    /// 多层环形地图配置：按层列出每层 cell 数量，携带层间错开范围、纵深/缩放曲线与滚轮速度参数。
    /// cell 默认尺寸与双态贴图配置于 RingLayer 预制体内，不在此配置（避免配置冗余与显示漂移）。
    /// 供 <see cref="RingMapGenerator"/> 加载后生成整图。
    /// </summary>
    [CreateAssetMenu(menuName = "Ring Map Config")]
    public class RingMapConfigSO : ScriptableObject
    {
        // ==================== 常量 ====================

        /// <summary>设计上限：满环 8 槽。</summary>
        public const int MAX_SLOTS_LIMIT = 8;

        // ==================== 多层配置 ====================

        /// <summary>
        /// 每层配置，数组顺序即层 index（0 为最上层/首层）。
        /// </summary>
        [Header("多层配置")]
        [SerializeField] private List<RingLayerConfig> _layers = new List<RingLayerConfig>();

        /// <summary>
        /// 固定槽位数量（默认 8，即 angleStep = 360 / maxSlots）。
        /// </summary>
        [SerializeField] private int _maxSlots = MAX_SLOTS_LIMIT;

        /// <summary>
        /// 层间错开角度范围 [min, max]，每层在此区间随机取一个偏移作为本层起始角。
        /// </summary>
        [SerializeField] private Vector2 _minMaxStagger = new Vector2(15f, 30f);

        // ==================== 纵深与曲线 ====================

        /// <summary>
        /// 层尺寸曲线：横轴为层 index，纵轴为该层 baseScale（首层最大，随 index 递减）。
        /// </summary>
        [Header("纵深与曲线")]
        [SerializeField] private AnimationCurve _layerSizeCurve =
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(7f, 0.35f));

        /// <summary>
        /// alpha 衰减曲线：横轴为 |浏览焦点 t - 层 index|，纵轴为该层 alpha（焦点层最高，距离增大衰减）。
        /// </summary>
        [SerializeField] private AnimationCurve _alphaByDistanceCurve =
            new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0.6f),
                new Keyframe(2f, 0.35f),
                new Keyframe(3f, 0.15f),
                new Keyframe(7f, 0.05f));

        /// <summary>
        /// 整图根节点缩放曲线：提交/浏览时按此曲线对根节点 scale 做 Tween（放大/缩小）。
        /// </summary>
        [SerializeField] private AnimationCurve _progressScaleCurve =
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 1.08f), new Keyframe(1f, 1f));

        // ==================== 交互参数 ====================

        /// <summary>
        /// 滚轮浏览速度：每次滚轮拨动改变浏览焦点 t 的幅度（层）。
        /// </summary>
        [Header("交互参数")]
        [SerializeField] private float _scrollSpeed = 0.5f;

        // ==================== 属性 ====================

        /// <summary>每层配置列表。</summary>
        public List<RingLayerConfig> Layers => _layers;

        /// <summary>固定槽位数量。</summary>
        public int MaxSlots => _maxSlots;

        /// <summary>层间错开角度范围 [min, max]。</summary>
        public Vector2 MinMaxStagger => _minMaxStagger;

        /// <summary>层尺寸曲线（随 index 递减）。</summary>
        public AnimationCurve LayerSizeCurve => _layerSizeCurve;

        /// <summary>alpha 随距离衰减曲线。</summary>
        public AnimationCurve AlphaByDistanceCurve => _alphaByDistanceCurve;

        /// <summary>整图根节点缩放曲线。</summary>
        public AnimationCurve ProgressScaleCurve => _progressScaleCurve;

        /// <summary>滚轮浏览速度。</summary>
        public float ScrollSpeed => _scrollSpeed;

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
