using ShowX.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpineTest.Battle
{
    /// <summary>
    /// 双人战斗演出编排器:唯一决策者。监听 B 键输入,以攻击方 B 的 clip 时间为
    /// 主时钟,在配置的时间阈值触发"打击 / 释放"节拍,并调用 A / B / Stage 执行。
    /// 状态流转:Ready → Windup(前摇)→ Strike(打击)→ Recover(后摇)→ Ready。
    /// </summary>
    public class SpineBattleDirector : MonoBehaviour
    {
        // ==================== 常量 ====================

        /// <summary>
        /// 日志标签
        /// </summary>
        private const string LOG_TAG = "SpineBattleDirector";

        /// <summary>
        /// 正常速度的时间刻度
        /// </summary>
        private const float NORMAL_TIME_SCALE = 1f;

        // ==================== 序列化字段 ====================

        [Header("引用")]
        [Tooltip("受击方 A(Spider)")]
        [SerializeField] private SpineActor _actorA;

        [Tooltip("攻击方 B(Spider_Corrupted)")]
        [SerializeField] private SpineActor _actorB;

        [Tooltip("双人补间舞台")]
        [SerializeField] private BattleStage _stage;

        [Header("节拍时间参数(以 B 攻击 clip 时间为基准,单位秒)")]
        [Tooltip("前摇结束 / 扑击起点:到达该时间触发打击节拍")]
        [SerializeField] private float _attackStartTime = 0.57f;

        [Tooltip("命中定格结束 / 后摇起点:到达该时间触发释放节拍")]
        [SerializeField] private float _attackEndTime = 0.80f;

        [Tooltip("A 受击 clip 内被定格采样的受击帧")]
        [SerializeField] private float _hitFrameTime = 0.12f;

        [Header("慢放")]
        [Tooltip("打击节拍期间 B 动画轨道的慢放倍率")]
        [SerializeField] private float _slowMoScale = 0.25f;

        [Header("攻击动画")]
        [Tooltip("B 的攻击动画 clip 名")]
        [SerializeField] private string _attackClipName = "Attack1";

        [Header("演出幕布Canvas")]
        [Tooltip("双人战斗演出慢动作幕布Canvas")]
        [SerializeField] private Canvas _battleCanvas;

        [Header("调试")]
        [Tooltip("启用后按 T 键打印 A / B 当前动画时间,便于精调节拍参数")]
        [SerializeField] private bool _enableDebugKeys = false;
        // ==================== 私有字段 ====================

        /// <summary>
        /// 当前演出阶段
        /// </summary>
        private SpineBattlePhase _phase = SpineBattlePhase.Ready;

        /// <summary>
        /// 打击节拍一次性守卫,保证单轮内只触发一次
        /// </summary>
        private bool _didStrike;

        /// <summary>
        /// 释放节拍一次性守卫,保证单轮内只触发一次
        /// </summary>
        private bool _didRecover;

        // ==================== Unity 生命周期 ====================

        private void Start()
        {
            if (!tryValidateRefs())
            {
                return;
            }
            _actorB.OnAnimationEnded += handleActorBAnimationEnded;

            if (_battleCanvas == null)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: _battleCanvas 为空,请在 Inspector 赋值");
                return;
            }
            _battleCanvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_actorB != null)
            {
                _actorB.OnAnimationEnded -= handleActorBAnimationEnded;
            }
        }

        private void Update()
        {
            if (_enableDebugKeys)
            {
                handleDebugKeys();
            }
            if (_phase == SpineBattlePhase.Ready)
            {
                if (isBPressedThisFrame())
                {
                    startPerformance();
                }
                return;
            }
            pollBeatThresholds();
        }

        // ==================== 公共方法 ====================

        /// <summary>
        /// 立即开始一轮演出(仅 Ready 态可调用,等价于按下 B 键)
        /// </summary>
        public void StartPerformance()
        {
            if (_phase != SpineBattlePhase.Ready)
            {
                XLogger.LogWarning(LOG_TAG, "StartPerformance: 演出进行中,忽略触发请求");
                return;
            }
            startPerformance();
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 校验角色与舞台引用,缺失时输出错误并中断流程
        /// </summary>
        private bool tryValidateRefs()
        {
            if (_actorA == null || _actorB == null)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: _actorA / _actorB 为空,请在 Inspector 赋值");
                return false;
            }
            if (_stage == null)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: _stage 为空,请在 Inspector 赋值");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 待机态触发演出:B 播放攻击动画,进入前摇阶段
        /// </summary>
        private void startPerformance()
        {
            _didStrike = false;
            _didRecover = false;
            _actorB.PlayAttack(_attackClipName);
            _phase = SpineBattlePhase.Windup;
            XLogger.LogInfo(LOG_TAG, "startPerformance: 开始一轮战斗演出");
        }

        /// <summary>
        /// 每帧轮询 B 的 clip 时间,越过阈值时触发对应节拍
        /// </summary>
        private void pollBeatThresholds()
        {
            float actorBTime = _actorB.CurrentAnimationTime;
            switch (_phase)
            {
                case SpineBattlePhase.Windup:
                    if (!_didStrike && actorBTime >= _attackStartTime)
                    {
                        triggerStrikeBeat();
                    }
                    break;
                case SpineBattlePhase.Strike:
                    if (!_didRecover && actorBTime >= _attackEndTime)
                    {
                        triggerRecoverBeat();
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 打击节拍:B 慢放 + A 受击定格 + 双人拉近放大
        /// </summary>
        private void triggerStrikeBeat()
        {
            _didStrike = true;
            _actorB.SetTimeScale(_slowMoScale);
            _actorA.FreezeHitAt(_hitFrameTime);
            _stage.FocusIn();
            _phase = SpineBattlePhase.Strike;
            _battleCanvas.gameObject.SetActive(true);
            XLogger.LogInfo(LOG_TAG, "triggerStrikeBeat: 打击节拍触发");
        }

        /// <summary>
        /// 释放节拍:B 恢复常速 + A 解除定格续播 + 双人回位
        /// </summary>
        private void triggerRecoverBeat()
        {
            _didRecover = true;
            _actorB.SetTimeScale(NORMAL_TIME_SCALE);
            _actorA.Resume();
            _stage.FocusOut();
            _phase = SpineBattlePhase.Recover;
            _battleCanvas.gameObject.SetActive(false);
            XLogger.LogInfo(LOG_TAG, "triggerRecoverBeat: 释放节拍触发");
        }

        /// <summary>
        /// B 攻击动画结束回调:A / B 回到待机,状态复位 Ready
        /// </summary>
        private void handleActorBAnimationEnded()
        {
            if (_phase == SpineBattlePhase.Ready)
            {
                return;
            }
            if (_phase == SpineBattlePhase.Strike)
            {
                // 攻击将结束仍未到释放点(极端参数),先补一次释放节拍
                triggerRecoverBeat();
            }
            if (_phase != SpineBattlePhase.Recover)
            {
                return;
            }
            _actorA.PlayIdle();
            _actorB.PlayIdle();
            _phase = SpineBattlePhase.Ready;
            XLogger.LogInfo(LOG_TAG, "handleActorBAnimationEnded: 演出结束,状态回 Ready");
        }

        /// <summary>
        /// 读取新输入系统本帧是否按下 B 键
        /// </summary>
        private bool isBPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }
            return keyboard.bKey.wasPressedThisFrame;
        }

        /// <summary>
        /// 调试按键处理:按 T 打印 A / B 当前动画时间
        /// </summary>
        private void handleDebugKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }
            if (keyboard.tKey.wasPressedThisFrame)
            {
                XLogger.LogInfo(LOG_TAG,
                    string.Format("handleDebugKeys: phase={0}, B.AnimationTime={1:F3}, A.AnimationTime={2:F3}",
                        _phase, _actorB.CurrentAnimationTime, _actorA.CurrentAnimationTime));
            }
        }
    }

    /// <summary>
    /// 双人战斗演出的阶段状态
    /// </summary>
    public enum SpineBattlePhase
    {
        /// <summary>待机:可接受 B 键触发新一轮演出</summary>
        Ready,

        /// <summary>前摇:攻击方蓄力,等待打击起点</summary>
        Windup,

        /// <summary>打击:慢放 + 受击定格 + 拉近,等待释放点</summary>
        Strike,

        /// <summary>后摇:恢复常速续播,等待动画自然结束</summary>
        Recover,
    }
}
