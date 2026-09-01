using System;
using System.Collections.Generic;
using ShowX.Utils;
using UnityEditor;
using UnityEngine;

namespace MapTest
{
    /// <summary>
    /// 多层环形地图整图控制器：从 <see cref="RingMapConfigSO"/> 实例化各 <see cref="RingLayer"/> 预制体
    /// （不直接创建 cell），维护激活层 activeLayerIndex 与缩放步进 offset（复用 _browseT 字段），订阅
    /// <see cref="RingMapEvents.OnCellClicked"/> 作为 cell 点击统一处理入口，并向各层广播视觉刷新。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class RingMapGenerator : MonoBehaviour
    {
        // ==================== 常量 ====================

        /// <summary>整圆角度（度）。</summary>
        private const float FULL_CIRCLE_ANGLE = 360f;

        /// <summary>完全错开基准系数：半格 cell 角度（0.5 表示 cellAngle / 2）。</summary>
        private const float HALF_CELL_RATIO = 0.5f;

        // ==================== 字段 ====================

        /// <summary>
        /// 多层地图配置。
        /// </summary>
        [Header("配置")]
        [SerializeField] private RingMapConfigSO _config;

        /// <summary>
        /// 生成的各层组件。
        /// </summary>
        [SerializeField] private List<RingLayer> _layers = new List<RingLayer>();

        /// <summary>
        /// 缩放步进显示值 offset（复用原浏览焦点字段）：层 i 缩放 = ratio^(i - offset)，offset ∈ [0, maxScaleOffset]，
        /// 每帧以 WheelTweenSpeed 向目标值 _browseTarget 平滑渐变，驱动全体层等比缩放，不改激活层。
        /// </summary>
        [SerializeField] private float _browseT;

        /// <summary>
        /// 缩放步进目标值 offset：由滚轮（每格增量 = WheelStep / TicksPerLevel）或左右键（±1 层）修改，
        /// 显示值 _browseT 在 Update 中向其渐变。
        /// </summary>
        [SerializeField] private float _browseTarget;

        /// <summary>
        /// 已提交激活层 index，Enable 贴图绑定此值。
        /// </summary>
        [SerializeField] private int _activeLayerIndex;

        /// <summary>
        /// 本组件 RectTransform。
        /// </summary>
        private RectTransform _rectTransform;

        // ==================== 事件 ====================

        /// <summary>
        /// 激活层推进事件（参数为方向 ±1），供交互层刷新 HUD。
        /// </summary>
        public event Action<int> LayerAdvanced;

        // ==================== 属性 ====================

        /// <summary>多层地图配置（只读）。</summary>
        public RingMapConfigSO Config => _config;

        /// <summary>缩放步进显示值 offset（只读，随目标值平滑渐变），层 i 缩放 = ratio^(i - offset)。</summary>
        public float ScaleOffset => _browseT;

        /// <summary>已提交激活层 index（只读）。</summary>
        public int ActiveLayerIndex => _activeLayerIndex;

        // ==================== Unity 生命周期 ====================

        /// <summary>
        /// 缓存自身 RectTransform。
        /// </summary>
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 订阅 cell 点击事件（与 OnDisable 成对，防静态事件泄漏）。
        /// </summary>
        private void OnEnable()
        {
            RingMapEvents.OnCellClicked += handleCellClicked;
        }

        /// <summary>
        /// 初始生成一次地图。
        /// </summary>
        private void Start()
        {
            Generate();
        }

        /// <summary>
        /// 每帧钳制目标值并把显示值 _browseT 以 WheelTweenSpeed 向目标渐变，
        /// 再将缩放 offset 与激活层广播给各层 SetVisual，刷新等比缩放/alpha（恒 1）/Enable 双态。
        /// </summary>
        private void Update()
        {
            if (_layers.Count == 0 || _config == null)
            {
                return;
            }
            _browseTarget = Mathf.Clamp(_browseTarget, 0f, _config.MaxScaleOffset);
            _browseT = Mathf.MoveTowards(_browseT, _browseTarget, _config.WheelTweenSpeed * Time.deltaTime);
            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i].SetVisual(_browseT, _activeLayerIndex, _config.ScaleRatio);
            }
        }

        /// <summary>
        /// 取消订阅 cell 点击事件（与 OnEnable 成对）。
        /// </summary>
        private void OnDisable()
        {
            RingMapEvents.OnCellClicked -= handleCellClicked;
        }

        // ==================== 生成与状态 ====================

        /// <summary>
        /// 按配置实例化各 RingLayer 预制体：把层 count 注入 RingLayer（其经 UiRingLayout 生成并布局 cell），
        /// 起始角按链式规则计算（第一层区间随机，后续层以上一层最后一个 cell 为锚点错开半格并叠加浮动），
        /// 大小/方向取默认（scale 由 SetVisual 按等比模型自动计算）。
        /// </summary>
        public void Generate()
        {
            clearLayers();
            if (_config == null)
            {
                XLogger.LogError("RingMapGenerator", "Generate: _config 为空");
                return;
            }
            if (_config.LayerPrefab == null)
            {
                XLogger.LogError("RingMapGenerator", "Generate: 配置未指定 RingLayer 预制体");
                return;
            }
            int layerCount = _config.Layers.Count;
            if (layerCount == 0)
            {
                XLogger.LogWarning("RingMapGenerator", "Generate: 配置未包含任何层");
                return;
            }
            int maxSlots = Mathf.Max(1, _config.MaxSlots);
            float prevStagger = 0f;
            int prevCount = 0;
            for (int i = 0; i < layerCount; i++)
            {
                RingLayerConfig cfg = _config.Layers[i];
                int count = cfg.Count;
                // 链式起始角：第一层区间随机，后续层以上一层最后一个 cell 为锚点完全错开 + 浮动
                float stagger = computeStagger(i, prevStagger, prevCount);
                prevStagger = stagger;
                // 上一层实际渲染的 cell 数（RingLayer 会将 count 钳制到 [0, maxSlots]），作为锚点计算基准
                prevCount = Mathf.Min(count, maxSlots);
                RingLayer layer = instantiateLayer(i, count, stagger);
                if (layer == null)
                {
                    continue;
                }
                _layers.Add(layer);
            }
            _activeLayerIndex = 0;
            _browseT = 0f;
            _browseTarget = 0f;
        }

        /// <summary>
        /// 推进激活层：dir=+1 进入下一层，dir=-1 退回上一层；两端无效。
        /// 同时把缩放目标值向同方向移动 ±1 层（显示值平滑渐变到对应层），通过 <see cref="LayerAdvanced"/> 通知交互层。
        /// </summary>
        public void AdvanceLayer(int dir)
        {
            if (_layers.Count == 0 || _config == null)
            {
                return;
            }
            int lastIndex = _layers.Count - 1;
            int newIndex = Mathf.Clamp(_activeLayerIndex + dir, 0, lastIndex);
            if (newIndex == _activeLayerIndex)
            {
                return;
            }
            _activeLayerIndex = newIndex;
            _browseTarget = Mathf.Clamp(_browseTarget + dir, 0f, _config.MaxScaleOffset);
            Action<int> handler = LayerAdvanced;
            if (handler != null)
            {
                handler(dir);
            }
        }

        /// <summary>
        /// 滚轮缩放：delta &gt; 0 全体层等比放大一级（offset 目标前进），delta &lt; 0 缩小；
        /// 仅修改目标值 _browseTarget，显示值 _browseT 在 Update 中平滑渐变；目标钳制到 [0, maxScaleOffset]，不改激活层。
        /// </summary>
        public void ZoomBy(float delta)
        {
            if (_config == null)
            {
                return;
            }
            _browseTarget = Mathf.Clamp(_browseTarget + delta, 0f, _config.MaxScaleOffset);
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 计算第 layerIndex 层的起始角：第一层在 FirstLayerStaggerRange 内随机（无上一层）；
        /// 后续层以上一层最后一个 cell 角度 + 半格 cell 角度为完全错开基准，再叠加 ±StaggerJitterCells 个 cell 的随机浮动。
        /// </summary>
        private float computeStagger(int layerIndex, float prevStagger, int prevCount)
        {
            if (layerIndex == 0)
            {
                Vector2 range = _config.FirstLayerStaggerRange;
                return UnityEngine.Random.Range(range.x, range.y);
            }
            int maxSlots = Mathf.Max(1, _config.MaxSlots);
            float angleStep = FULL_CIRCLE_ANGLE / maxSlots;
            float dir = _config.IsClockwise ? 1f : -1f;
            // 上一层最后一个 cell 的角度（含其起始角、方向与槽位偏移）
            float prevLastAngle = prevStagger + dir * (prevCount - 1) * angleStep;
            // 完全错开基准：在最后一个 cell 基础上再多转半格
            float baseAngle = prevLastAngle + dir * angleStep * HALF_CELL_RATIO;
            float jitter = UnityEngine.Random.Range(-_config.StaggerJitterCells, _config.StaggerJitterCells) * angleStep;
            return baseAngle + jitter;
        }

        /// <summary>
        /// 实例化单层 RingLayer（直接实例化组件，其 GameObject 一并生成）并注入层配置
        /// （层 index/count/错开等），配置完成后激活（RingLayer 经 UiRingLayout 自行生成并布局 cell）。
        /// </summary>
        private RingLayer instantiateLayer(int layerIndex, int count, float stagger)
        {
            RingLayer layer = Instantiate(_config.LayerPrefab, transform);
            layer.name = $"RingLayer_{layerIndex}";
            layer.gameObject.SetActive(false);
            // UiRingLayout 在 RingLayer 初始化前检查，缺失时销毁该层并跳过
            if (layer.Layout == null)
            {
                XLogger.LogError("RingMapGenerator", $"instantiateLayer: 层 {layerIndex} 预制体缺少 UiRingLayout");
                Destroy(layer.gameObject);
                return null;
            }
            // 大小取默认：scale 由 SetVisual 按等比模型自动计算；方向与满环槽位来自 RingMapConfigSO
            float layerDirection = _config.IsClockwise ? 1f : -1f;
            layer.Init(layerIndex, count, 1f, layerDirection, stagger, _config.MaxSlots);
            layer.gameObject.SetActive(true);
            return layer;
        }

        /// <summary>
        /// 销毁已生成的各层。
        /// </summary>
        private void clearLayers()
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                if (_layers[i] != null)
                {
                    Destroy(_layers[i].gameObject);
                }
            }
            _layers.Clear();
        }

        /// <summary>
        /// cell 点击统一处理入口：先输出层数与序号日志，后续可扩展路由。
        /// </summary>
        private void handleCellClicked(int layerIndex, int cellIndex)
        {
            XLogger.LogInfo("RingMapGenerator", $"cell clicked layer={layerIndex} index={cellIndex}");
        }

        // ==================== 编辑器工具 ====================

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器按钮：重新生成地图。
        /// </summary>
        [Button("重新生成")]
        private void regenerate()
        {
            Generate();
        }

        /// <summary>
        /// 编辑器按钮：将当前配置写回资产并保存（配置为唯一数据源，保存即持久化）。
        /// </summary>
        [Button("保存到配置文件")]
        private void saveToConfig()
        {
            if (_config == null)
            {
                XLogger.LogError("RingMapGenerator", "saveToConfig: _config 为空，无法保存");
                return;
            }
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            XLogger.LogInfo("RingMapGenerator", "saveToConfig: 已保存配置资产");
        }
#endif
    }
}
