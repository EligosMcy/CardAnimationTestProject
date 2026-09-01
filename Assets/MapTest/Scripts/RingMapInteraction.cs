using System;
using ShowX.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MapTest
{
    /// <summary>
    /// 环形地图交互控制（InputActionProperty，长按触发）：按住键盘 ↑ 长满阈值进入下一层、按住 ↓ 长满阈值退回上一层
    /// （两端无效，可回退；松手早于阈值不切换），长按期间 <see cref="_pressProgressImage"/> 的 fillAmount 实时显示进度 0→1；
    /// 滚轮缩放整图（offset 前进 = 全体层等比放大一级，后退 = 缩小，上下限由配置钳制）。
    /// 层切换输入通过两个 InputActionProperty 配置（默认内联绑定 ↑/↓），便于在 Inspector 中改绑其他按键；
    /// 更新两个 TextMeshProUGUI 指示（方向/缩放状态）。
    /// </summary>
    public class RingMapInteraction : MonoBehaviour
    {
        // ==================== 常量 ====================

        /// <summary>忽略滚轮值的阈值。</summary>
        private const float SCROLL_IGNORE_THRESHOLD = 0.01f;

        /// <summary>缩放百分比换算基数。</summary>
        private const float PERCENT_MULTIPLIER = 100f;

        /// <summary>长按阈值下限（防止误配为 0 导致除零/立即触发）。</summary>
        private const float MIN_LONG_PRESS_DURATION = 0.01f;

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
        /// 状态指示 TextMeshPro（显示激活层数与 0 层当前缩放百分比）。
        /// </summary>
        [SerializeField] private TextMeshProUGUI _statusText;

        /// <summary>
        /// 进入下一层输入动作（按住长满阈值后切一层；默认内联绑定键盘 ↑，可在 Inspector 改绑其他按键）。
        /// </summary>
        [Header("层切换输入")]
        [SerializeField] private InputActionProperty _nextLayerAction = new InputActionProperty(new InputAction("NextLayer", InputActionType.Button, "<Keyboard>/upArrow"));

        /// <summary>
        /// 退回上一层输入动作（按住长满阈值后切一层；默认内联绑定键盘 ↓，可在 Inspector 改绑其他按键）。
        /// </summary>
        [SerializeField] private InputActionProperty _previousLayerAction = new InputActionProperty(new InputAction("PreviousLayer", InputActionType.Button, "<Keyboard>/downArrow"));

        /// <summary>
        /// 长按进度 Image（Image.Type 需为 Filled，fillAmount 随长按进度 0→1 变化，松手清零）。
        /// </summary>
        [Header("长按进度")]
        [SerializeField] private Image _pressProgressImage;

        /// <summary>
        /// 长按触发阈值（秒）：按住 ↑/↓ 达到该时长后执行一次层切换。
        /// </summary>
        [SerializeField] private float _longPressDuration = 0.5f;

        /// <summary>
        /// 整图根节点缩放曲线（已停用：根节点缩放 Tween 已移除，保留字段兼容旧序列化数据）。
        /// </summary>
        [Header("交互参数（默认从配置读取）")]
        [SerializeField] private AnimationCurve _progressScaleCurve;

        /// <summary>
        /// 滚轮缩放步长：每次滚轮拨动推进 offset 的幅度（层，运行期从配置 WheelStep / TicksPerLevel 计算覆盖，
        /// 默认 1/5 = 0.2，滚满 5 格推进一整层）。
        /// </summary>
        [SerializeField] private float _scrollSpeed = 0.5f;

        // ==================== 运行时状态 ====================

        /// <summary>输入控制包装类（RingMapControls.inputactions 生成，仅用于滚轮缩放）。</summary>
        private RingMapControls _controls;

        /// <summary>最近一次层移动是否向下（true=向下进入深层，false=向上退回表层）。</summary>
        private bool _lastMoveDown = true;

        /// <summary>是否正在长按中（按住 ↑ 或 ↓ 未松开）。</summary>
        private bool _isPressing;

        /// <summary>当前长按方向（1=进入下一层，-1=退回上一层），仅在 _isPressing 时有效。</summary>
        private int _pressDir;

        /// <summary>长按起始时间（Time.unscaledTime，不受时间缩放影响）。</summary>
        private float _pressStartTime;

        /// <summary>当前长按是否已执行层切换（达到阈值后置 true，防止重复触发）。</summary>
        private bool _pressExecuted;

        // ==================== Unity 生命周期 ====================

        /// <summary>
        /// 订阅两个层切换 InputActionProperty 的按下/松开事件并启用动作，创建滚轮输入并订阅生成器推进事件。
        /// </summary>
        private void OnEnable()
        {
            bindAction(_nextLayerAction, onNextLayerPressed, onNextLayerReleased);
            bindAction(_previousLayerAction, onPreviousLayerPressed, onPreviousLayerReleased);
            _controls = new RingMapControls();
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
        /// 每帧推进长按计时与进度显示，并刷新状态指示（缩放百分比随 offset 实时变化）。
        /// </summary>
        private void Update()
        {
            tickLongPress();
            updateHud();
        }

        /// <summary>
        /// 取消订阅并禁用输入，避免残留状态。
        /// </summary>
        private void OnDisable()
        {
            unbindAction(_nextLayerAction, onNextLayerPressed, onNextLayerReleased);
            unbindAction(_previousLayerAction, onPreviousLayerPressed, onPreviousLayerReleased);
            if (_generator != null)
            {
                _generator.LayerAdvanced -= onLayerAdvanced;
            }
            if (_controls != null)
            {
                _controls.Map.Wheel.performed -= onWheel;
                _controls.Map.Disable();
                _controls.Dispose();
                _controls = null;
            }
        }

        // ==================== 输入处理 ====================

        /// <summary>
        /// ↑ 键按下：开始长按计时（长满阈值后进入下一层）。
        /// </summary>
        private void onNextLayerPressed(InputAction.CallbackContext context)
        {
            beginPress(1);
        }

        /// <summary>
        /// ↓ 键按下：开始长按计时（长满阈值后退回上一层）。
        /// </summary>
        private void onPreviousLayerPressed(InputAction.CallbackContext context)
        {
            beginPress(-1);
        }

        /// <summary>
        /// ↑ 键松开：结束长按（未满阈值则不切换）。
        /// </summary>
        private void onNextLayerReleased(InputAction.CallbackContext context)
        {
            endPress(1);
        }

        /// <summary>
        /// ↓ 键松开：结束长按（未满阈值则不切换）。
        /// </summary>
        private void onPreviousLayerReleased(InputAction.CallbackContext context)
        {
            endPress(-1);
        }

        /// <summary>
        /// 记录一次长按开始：同一时间只允许一个方向的长按，已在按则忽略。
        /// </summary>
        private void beginPress(int dir)
        {
            if (_isPressing)
            {
                return;
            }
            _isPressing = true;
            _pressDir = dir;
            _pressStartTime = Time.unscaledTime;
            _pressExecuted = false;
        }

        /// <summary>
        /// 结束指定方向的长按：与当前长按方向一致才清空（防止另一方向松手误清），未满阈值时此处不会切换。
        /// </summary>
        private void endPress(int dir)
        {
            if (!_isPressing || _pressDir != dir)
            {
                return;
            }
            _isPressing = false;
            _pressExecuted = false;
            setPressFill(0f);
        }

        /// <summary>
        /// 滚轮缩放：前进（y&gt;0）全体层等比放大一级、后退（y&lt;0）缩小；不改激活层，并更新方向指示与缩放状态。
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
                _generator.ZoomBy(Mathf.Sign(scroll.y) * _scrollSpeed);
            }
            updateHud();
        }

        /// <summary>
        /// 激活层推进回调：记录方向并刷新 HUD。
        /// </summary>
        private void onLayerAdvanced(int dir)
        {
            _lastMoveDown = dir > 0;
            updateHud();
        }

        // ==================== 长按进度 ====================

        /// <summary>
        /// 每帧长按计时：满阈值执行一次层切换并锁定，实时把进度写入进度 Image（未长按时清零）。
        /// </summary>
        private void tickLongPress()
        {
            if (!_isPressing)
            {
                setPressFill(0f);
                return;
            }
            float elapsed = Time.unscaledTime - _pressStartTime;
            float duration = Mathf.Max(_longPressDuration, MIN_LONG_PRESS_DURATION);
            if (!_pressExecuted && elapsed >= duration)
            {
                _pressExecuted = true;
                if (_generator != null)
                {
                    _generator.AdvanceLayer(_pressDir);
                }
            }
            setPressFill(Mathf.Clamp01(elapsed / duration));
        }

        /// <summary>
        /// 设置长按进度 Image 的 fillAmount（未配置时忽略）。
        /// </summary>
        private void setPressFill(float amount)
        {
            if (_pressProgressImage != null)
            {
                _pressProgressImage.fillAmount = amount;
            }
        }

        // ==================== HUD ====================

        /// <summary>
        /// 更新两个 TextMeshProUGUI：方向指示（向下/向上）与状态指示（激活层 + 0 层缩放百分比）。
        /// </summary>
        private void updateHud()
        {
            if (_directionText != null)
            {
                _directionText.text = _lastMoveDown ? "向下" : "向上";
            }
            if (_statusText != null && _generator != null && _generator.Config != null)
            {
                float layer0Scale = Mathf.Pow(_generator.Config.ScaleRatio, -_generator.ScaleOffset);
                int percent = Mathf.RoundToInt(PERCENT_MULTIPLIER * layer0Scale);
                _statusText.text = $"激活:{_generator.ActiveLayerIndex}  缩放:{percent}%";
            }
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 订阅单个 InputActionProperty 的按下（performed）/松开（canceled）事件并启用动作（兼容内联动作与资产引用两种模式）。
        /// </summary>
        private void bindAction(InputActionProperty property, Action<InputAction.CallbackContext> onPressed, Action<InputAction.CallbackContext> onReleased)
        {
            InputAction action = property.action;
            if (action == null)
            {
                XLogger.LogError("RingMapInteraction", "bindAction: 未配置输入动作，请在 Inspector 中绑定按键");
                return;
            }
            action.performed += onPressed;
            action.canceled += onReleased;
            action.Enable();
        }

        /// <summary>
        /// 取消订阅并禁用输入动作（与 bindAction 成对，防止残留）。
        /// </summary>
        private void unbindAction(InputActionProperty property, Action<InputAction.CallbackContext> onPressed, Action<InputAction.CallbackContext> onReleased)
        {
            InputAction action = property.action;
            if (action == null)
            {
                return;
            }
            action.performed -= onPressed;
            action.canceled -= onReleased;
            action.Disable();
        }

        /// <summary>
        /// 从生成器配置读取滚轮缩放步长 WheelStep 与每格分数单位 TicksPerLevel，
        /// 计算每格拨动的 offset 增量 = WheelStep / TicksPerLevel（配置为唯一数据源，运行期覆盖 Inspector 内联值）。
        /// </summary>
        private void loadFromConfig()
        {
            if (_generator == null || _generator.Config == null)
            {
                return;
            }
            int ticksPerLevel = Mathf.Max(1, _generator.Config.WheelTicksPerLevel);
            _scrollSpeed = _generator.Config.WheelStep / ticksPerLevel;
        }
    }
}
