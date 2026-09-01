using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MapTest
{
    /// <summary>
    /// 环形地图交互控制（InputActionAsset，全部短按）：左键短按进入下一层、右键短按退回上一层（两端无效，可回退），
    /// 滚轮在 [activeLayerIndex, 末层] 内浏览预览（不改激活层）；左键命中 cell Button 时不切换（仅 cell 点击上报）。
    /// 提交/浏览期间按 progressScaleCurve 对整图根节点 scale 做 Tween，并更新两个 TextMeshProUGUI 指示（方向/状态）。
    /// </summary>
    public class RingMapInteraction : MonoBehaviour
    {
        // ==================== 常量 ====================

        /// <summary>根节点缩放 Tween 时长（秒）。</summary>
        private const float SCALE_TWEEN_DURATION = 0.25f;

        /// <summary>滚轮浏览缩放 Tween 强度。</summary>
        private const float SCROLL_SCALE_INTENSITY = 0.35f;

        /// <summary>提交缩放 Tween 强度。</summary>
        private const float SUBMIT_SCALE_INTENSITY = 1f;

        /// <summary>忽略滚轮值的阈值。</summary>
        private const float SCROLL_IGNORE_THRESHOLD = 0.01f;

        // ==================== 字段 ====================

        /// <summary>
        /// 整图控制器引用。
        /// </summary>
        [Header("引用")]
        [SerializeField] private RingMapGenerator _generator;

        /// <summary>
        /// 方向指示 TextMeshPro（显示最近一次层移动方向：向下/向上）。
        /// </summary>
        [SerializeField] private TextMeshProUGUI _directionText;

        /// <summary>
        /// 状态指示 TextMeshPro（显示激活层数与当前显示层数）。
        /// </summary>
        [SerializeField] private TextMeshProUGUI _statusText;

        /// <summary>
        /// 整图根节点缩放曲线（提交/浏览时 Tween），默认从配置读取。
        /// </summary>
        [Header("交互参数（默认从配置读取）")]
        [SerializeField] private AnimationCurve _progressScaleCurve;

        /// <summary>
        /// 滚轮浏览速度：每次滚轮拨动改变浏览焦点 t 的幅度（层）。
        /// </summary>
        [SerializeField] private float _scrollSpeed = 0.5f;

        // ==================== 运行时状态 ====================

        /// <summary>输入控制包装类（RingMapControls.inputactions 生成）。</summary>
        private RingMapControls _controls;

        /// <summary>根节点缩放 Tween 协程引用。</summary>
        private Coroutine _scaleTween;

        /// <summary>整图根节点 Transform（Tween 目标）。</summary>
        private Transform _rootTransform;

        /// <summary>最近一次层移动是否向下（true=向下进入深层，false=向上退回表层）。</summary>
        private bool _lastMoveDown = true;

        // ==================== Unity 生命周期 ====================

        /// <summary>
        /// 缓存根节点 Transform。
        /// </summary>
        private void Awake()
        {
            _rootTransform = _generator != null ? _generator.transform : transform;
        }

        /// <summary>
        /// 创建并启用输入动作，订阅 performed 回调与生成器推进事件。
        /// </summary>
        private void OnEnable()
        {
            _controls = new RingMapControls();
            _controls.Map.LeftClick.performed += onLeftClick;
            _controls.Map.RightClick.performed += onRightClick;
            _controls.Map.Wheel.performed += onWheel;
            _controls.Map.Enable();
            if (_generator != null)
            {
                _generator.LayerAdvanced += onLayerAdvanced;
            }
        }

        /// <summary>
        /// 从配置读取交互参数并刷新一次 HUD。
        /// </summary>
        private void Start()
        {
            loadFromConfig();
            updateHud();
        }

        /// <summary>
        /// 每帧刷新状态指示（显示层数 round(t) 随浏览实时变化）。
        /// </summary>
        private void Update()
        {
            updateHud();
        }

        /// <summary>
        /// 取消订阅、禁用输入并停止 Tween，避免残留状态。
        /// </summary>
        private void OnDisable()
        {
            if (_generator != null)
            {
                _generator.LayerAdvanced -= onLayerAdvanced;
            }
            if (_controls != null)
            {
                _controls.Map.LeftClick.performed -= onLeftClick;
                _controls.Map.RightClick.performed -= onRightClick;
                _controls.Map.Wheel.performed -= onWheel;
                _controls.Map.Disable();
                _controls.Dispose();
                _controls = null;
            }
            if (_scaleTween != null)
            {
                StopCoroutine(_scaleTween);
                _scaleTween = null;
            }
        }

        // ==================== 输入处理 ====================

        /// <summary>
        /// 左键短按：若指针落在 cell Button 上则忽略（仅 cell 点击上报），否则进入下一层。
        /// </summary>
        private void onLeftClick(InputAction.CallbackContext context)
        {
            if (isPointerOverCell())
            {
                return;
            }
            if (_generator != null)
            {
                _generator.AdvanceLayer(1);
            }
        }

        /// <summary>
        /// 右键短按：退回上一层（最上层无效由生成器内部处理）。
        /// </summary>
        private void onRightClick(InputAction.CallbackContext context)
        {
            if (_generator != null)
            {
                _generator.AdvanceLayer(-1);
            }
        }

        /// <summary>
        /// 滚轮浏览：前进（y&gt;0）向深层、后退（y&lt;0）向表层，改变 browseT 不改激活层；并更新方向指示与根 Tween。
        /// </summary>
        private void onWheel(InputAction.CallbackContext context)
        {
            Vector2 scroll = context.ReadValue<Vector2>();
            if (Mathf.Abs(scroll.y) < SCROLL_IGNORE_THRESHOLD)
            {
                return;
            }
            _lastMoveDown = scroll.y > 0f;
            if (_generator != null)
            {
                _generator.BrowseBy(Mathf.Sign(scroll.y) * _scrollSpeed);
            }
            playScaleTween(SCROLL_SCALE_INTENSITY);
            updateHud();
        }

        /// <summary>
        /// 激活层推进回调：记录方向、触发提交 Tween 并刷新 HUD。
        /// </summary>
        private void onLayerAdvanced(int dir)
        {
            _lastMoveDown = dir > 0;
            playScaleTween(SUBMIT_SCALE_INTENSITY);
            updateHud();
        }

        /// <summary>
        /// 检测当前指针是否落在某个 cell Button 上（命中时左键仅触发 cell 点击，不切换层）。
        /// </summary>
        private bool isPointerOverCell()
        {
            if (EventSystem.current == null)
            {
                return false;
            }
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = Mouse.current.position.ReadValue();
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject != null && results[i].gameObject.GetComponentInParent<RingCell>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        // ==================== HUD ====================

        /// <summary>
        /// 更新两个 TextMeshProUGUI：方向指示（向下/向上）与状态指示（激活层 + 显示层 round(t)）。
        /// </summary>
        private void updateHud()
        {
            if (_directionText != null)
            {
                _directionText.text = _lastMoveDown ? "向下" : "向上";
            }
            if (_statusText != null && _generator != null)
            {
                int shown = Mathf.RoundToInt(_generator.BrowseT);
                _statusText.text = $"激活:{_generator.ActiveLayerIndex}  显示:{shown}";
            }
        }

        // ==================== 根节点缩放 Tween ====================

        /// <summary>
        /// 按 progressScaleCurve 对根节点 scale 做 Tween（放大/缩小），打断上一次 Tween。
        /// </summary>
        private void playScaleTween(float intensity)
        {
            if (_rootTransform == null || _progressScaleCurve == null || _progressScaleCurve.length == 0)
            {
                return;
            }
            if (_scaleTween != null)
            {
                StopCoroutine(_scaleTween);
            }
            _scaleTween = StartCoroutine(scaleTweenRoutine(intensity));
        }

        /// <summary>
        /// 根节点缩放 Tween 协程：baseScale * (1 + (curve - 1) * intensity)，结束恢复基准。
        /// </summary>
        private IEnumerator scaleTweenRoutine(float intensity)
        {
            Vector3 baseScale = _rootTransform.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / SCALE_TWEEN_DURATION;
                float curveValue = _progressScaleCurve.Evaluate(Mathf.Clamp01(t));
                float factor = 1f + (curveValue - 1f) * intensity;
                _rootTransform.localScale = baseScale * factor;
                yield return null;
            }
            _rootTransform.localScale = baseScale;
            _scaleTween = null;
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 从生成器配置读取 scrollSpeed 与根节点缩放曲线（配置为唯一数据源，运行期覆盖 Inspector 内联值）。
        /// </summary>
        private void loadFromConfig()
        {
            if (_generator == null || _generator.Config == null)
            {
                return;
            }
            _scrollSpeed = _generator.Config.ScrollSpeed;
            _progressScaleCurve = _generator.Config.ProgressScaleCurve;
        }
    }
}
