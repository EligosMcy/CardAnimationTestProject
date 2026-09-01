using System;
using System.Collections.Generic;
using ShowX.Utils;
using UnityEditor;
using UnityEngine;

namespace MapTest
{
    /// <summary>
    /// 多层环形地图整图控制器：从 <see cref="RingMapConfigSO"/> 实例化各 <see cref="RingLayer"/> 预制体
    /// （不直接创建 cell），维护激活层 activeLayerIndex 与连续浏览焦点 browseT，订阅
    /// <see cref="RingMapEvents.OnCellClicked"/> 作为 cell 点击统一处理入口，并向各层广播视觉刷新。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class RingMapGenerator : MonoBehaviour
    {
        // ==================== 字段 ====================

        /// <summary>
        /// 多层地图配置。
        /// </summary>
        [Header("配置")]
        [SerializeField] private RingMapConfigSO _config;

        /// <summary>
        /// RingLayer 预制体（内含 UiRingLayout、cell 预制体与双态贴图引用），生成器只实例化不拼装 cell。
        /// </summary>
        [SerializeField] private GameObject _layerPrefab;

        /// <summary>
        /// 生成的各层组件。
        /// </summary>
        [SerializeField] private List<RingLayer> _layers = new List<RingLayer>();

        /// <summary>
        /// 连续浏览焦点（视觉深度），取值范围 [activeLayerIndex, 末层]，由滚轮改变，不改激活层。
        /// </summary>
        [SerializeField] private float _browseT;

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
        /// 激活层推进事件（参数为方向 ±1），供交互层触发根节点 progressScale Tween。
        /// </summary>
        public event Action<int> LayerAdvanced;

        // ==================== 属性 ====================

        /// <summary>多层地图配置（只读）。</summary>
        public RingMapConfigSO Config => _config;

        /// <summary>连续浏览焦点（只读）。</summary>
        public float BrowseT => _browseT;

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
        /// 每帧把浏览焦点与激活层广播给各层 SetVisual，刷新缩放/alpha/Enable 双态。
        /// </summary>
        private void Update()
        {
            if (_layers.Count == 0 || _config == null)
            {
                return;
            }
            int lastIndex = _layers.Count - 1;
            _browseT = Mathf.Clamp(_browseT, _activeLayerIndex, lastIndex);
            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i].SetVisual(_browseT, _activeLayerIndex, _config.LayerSizeCurve, _config.AlphaByDistanceCurve);
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
        /// 按配置实例化各 RingLayer 预制体：把 count 注入其 UiRingLayout，错开在全局范围内随机，
        /// 大小/方向取默认（baseScale 由 SetVisual 按 layerSizeCurve 自动计算），随后各层自行回填 cell 索引。
        /// </summary>
        public void Generate()
        {
            clearLayers();
            if (_config == null)
            {
                XLogger.LogError("RingMapGenerator", "Generate: _config 为空");
                return;
            }
            if (_layerPrefab == null)
            {
                XLogger.LogError("RingMapGenerator", "Generate: _layerPrefab 为空");
                return;
            }
            int layerCount = _config.Layers.Count;
            if (layerCount == 0)
            {
                XLogger.LogWarning("RingMapGenerator", "Generate: 配置未包含任何层");
                return;
            }
            int maxSlots = Mathf.Max(1, _config.MaxSlots);
            Vector2 staggerRange = _config.MinMaxStagger;
            for (int i = 0; i < layerCount; i++)
            {
                RingLayerConfig cfg = _config.Layers[i];
                int count = Mathf.Clamp(cfg.Count, 0, maxSlots);
                if (count != cfg.Count)
                {
                    XLogger.LogWarning("RingMapGenerator", $"Generate: 层 {i} count={cfg.Count} 越界，已钳制到 [0, {maxSlots}]");
                }
                // 层间错开：全局 [minStagger, maxStagger] 随机取，允许相邻层部分重叠
                float stagger = UnityEngine.Random.Range(staggerRange.x, staggerRange.y);
                GameObject layerGO = instantiateLayer(i, count, maxSlots, stagger);
                if (layerGO == null)
                {
                    continue;
                }
                if (!layerGO.TryGetComponent<RingLayer>(out RingLayer layer))
                {
                    XLogger.LogError("RingMapGenerator", $"Generate: 层 {i} 缺少 RingLayer 组件，已跳过");
                    Destroy(layerGO);
                    continue;
                }
                _layers.Add(layer);
            }
            _activeLayerIndex = 0;
            _browseT = 0f;
        }

        /// <summary>
        /// 推进激活层：dir=+1 进入下一层，dir=-1 退回上一层；两端无效。
        /// 同时将浏览焦点钳到新 [activeLayerIndex, 末层]，并通过 <see cref="LayerAdvanced"/> 通知交互层。
        /// </summary>
        public void AdvanceLayer(int dir)
        {
            if (_layers.Count == 0)
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
            _browseT = Mathf.Clamp(_browseT, _activeLayerIndex, lastIndex);
            Action<int> handler = LayerAdvanced;
            if (handler != null)
            {
                handler(dir);
            }
        }

        /// <summary>
        /// 滚轮浏览：改变 browseT，钳制到 [activeLayerIndex, 末层] 由 Update 保证；不改激活层。
        /// </summary>
        public void BrowseBy(float delta)
        {
            _browseT += delta;
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 实例化单层预制体并注入 count/槽位/错开参数，配置完成后激活（UiRingLayout 按新 count 同步 cell）。
        /// </summary>
        private GameObject instantiateLayer(int layerIndex, int count, int maxSlots, float stagger)
        {
            GameObject layerGO = Instantiate(_layerPrefab, transform);
            layerGO.name = $"RingLayer_{layerIndex}";
            layerGO.SetActive(false);
            if (!layerGO.TryGetComponent<UiRingLayout>(out UiRingLayout layout))
            {
                XLogger.LogError("RingMapGenerator", $"instantiateLayer: 层 {layerIndex} 预制体缺少 UiRingLayout");
                Destroy(layerGO);
                return null;
            }
            layout.MaxSlots = maxSlots;
            layout.Count = count;
            layout.StartSlot = 0;
            layout.StartAngleOffset = stagger;
            layout.IsClockwise = true;
            if (!layerGO.TryGetComponent<RingLayer>(out RingLayer layer))
            {
                XLogger.LogError("RingMapGenerator", $"instantiateLayer: 层 {layerIndex} 预制体缺少 RingLayer");
                Destroy(layerGO);
                return null;
            }
            // 大小/方向取默认：baseScale 由 SetVisual 按 layerSizeCurve 自动计算
            layer.Init(layout, layerIndex, count, 1f, 1f, stagger);
            layerGO.SetActive(true);
            return layerGO;
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
