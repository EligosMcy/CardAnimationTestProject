using ShowX.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpineTest.Battle
{
    /// <summary>
    /// 战斗演出编排器:唯一决策者。监听 B 键输入,以当前演出对攻击方攻击 clip 的
    /// AnimationTime 在前摇阶段触发"打击节拍"(双定格 + 瞬时聚焦 + 幕布开 + 相机晃动起),
    /// 随后进入固定时长的表现窗口(定格扩放大 + 相机晃动冲击);窗口到时驱动"恢复节拍"
    /// (定格收缩回 Home,回位完成后才解除定格常速续播),并驱动 Stage 执行挂载聚焦、
    /// 组缩放与相机晃动;演出对由 SpineBattlePair 枚举选择,不出现硬编码角色名。
    /// 全部数值参数读取 SpineBattleSettings(单一配置源)。
    /// 状态流转:Ready → Windup(前摇)→ Strike(打击定格/表现窗口)→ Recover(定格回位/续播)→ Ready。
    /// </summary>
    public class SpineBattleDirector : MonoBehaviour
    {
        // ==================== 常量 ====================

        /// <summary>
        /// 日志标签
        /// </summary>
        private const string LOG_TAG = "SpineBattleDirector";

        // ==================== 序列化字段 ====================

        [Header("配置资产")]
        [Tooltip("演出参数单一配置源(节拍/窗口/回位/倍率/相机晃动等);修改该资产即可整体调整演出")]
        [SerializeField] private SpineBattleSettings _settings;

        [Header("引用")]
        [Tooltip("战斗舞台执行器(挂载/摆位/相机晃动/回位)")]
        [SerializeField] private BattleStage _stage;

        [Tooltip("当前使用的演出对(默认 Pair_AB = SpiderA 受击 / SpiderB 攻击)")]
        [SerializeField] private SpineBattlePair _pair = SpineBattlePair.Pair_AB;

        [Tooltip("演出对 AB 的角色引用:Defender 受击 / Attacker 攻击")]
        [SerializeField] private PairSetup _pairAB;

        [Tooltip("演出对 CD 的角色引用:Defender 受击 / Attacker 攻击(预留)")]
        [SerializeField] private PairSetup _pairCD;

        [Tooltip("演出幕布 Canvas('--- Forward UI')")]
        [SerializeField] private Canvas _battleCanvas;

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
        /// 恢复节拍一次性守卫,保证单轮内只触发一次
        /// </summary>
        private bool _didRecover;

        /// <summary>
        /// 表现窗口计时协程
        /// </summary>
        private Coroutine _presentationCoroutine;

        /// <summary>
        /// 攻击方动画是否已播完(收尾双条件之一)
        /// </summary>
        private bool _attackerAnimationEnded;

        /// <summary>
        /// Stage 回位补间是否已完成(收尾双条件之一)
        /// </summary>
        private bool _returnHomeCompleted;

        /// <summary>
        /// 回位完成后是否已解除定格续播(单次守卫,保证只续播一次)
        /// </summary>
        private bool _didResumeAfterReturn;

        /// <summary>
        /// 本轮演出的受击方
        /// </summary>
        private SpineActor _roundDefender;

        /// <summary>
        /// 本轮演出的攻击方
        /// </summary>
        private SpineActor _roundAttacker;

        /// <summary>
        /// 当前已订阅动画结束事件的攻击方,用于换对时重绑
        /// </summary>
        private SpineActor _boundAttacker;

        // ==================== Unity 生命周期 ====================

        private void Start()
        {
            if (!tryValidateRefs())
            {
                return;
            }
            _battleCanvas.gameObject.SetActive(false);
            bindAttackerAnimation(getCurrentPair().Attacker);
            _stage.OnReturnHomeCompleted += handleReturnHomeCompleted;
            XLogger.LogInfo(LOG_TAG, "Start: 导演初始化完成,等待 B 键触发演出");
        }

        private void OnDestroy()
        {
            unbindAttackerAnimation();
            stopPresentationTimer();
            if (_stage != null)
            {
                _stage.OnReturnHomeCompleted -= handleReturnHomeCompleted;
            }
        }

        private void Update()
        {
            if (_settings != null && _settings.EnableDebugKeys)
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
        /// 校验配置资产、舞台、幕布与当前演出对引用,缺失时输出错误并中断流程
        /// </summary>
        private bool tryValidateRefs()
        {
            if (_settings == null)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: _settings 为空,请在 Inspector 挂载 SpineBattleCinematicSettings 资产");
                return false;
            }
            if (_settings.PresentationDuration <= 0f)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: PresentationDuration 必须大于 0,当前值=" + _settings.PresentationDuration);
                return false;
            }
            if (_settings.ReturnDuration <= 0f)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: ReturnDuration 必须大于 0,当前值=" + _settings.ReturnDuration);
                return false;
            }
            if (_stage == null)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: _stage 为空,请在 Inspector 赋值");
                return false;
            }
            if (_battleCanvas == null)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: _battleCanvas 为空,请在 Inspector 赋值");
                return false;
            }
            PairSetup pair = getCurrentPair();
            if (pair.Defender == null || pair.Attacker == null)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: 当前演出对(" + _pair + ")的 Defender/Attacker 未接线,请在 Inspector 赋值");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 按当前演出对枚举取对应的受击/攻击引用
        /// </summary>
        private PairSetup getCurrentPair()
        {
            if (_pair == SpineBattlePair.Pair_CD)
            {
                return _pairCD;
            }
            return _pairAB;
        }

        /// <summary>
        /// 待机态触发演出:攻击方播放攻击动画,进入前摇阶段
        /// </summary>
        private void startPerformance()
        {
            PairSetup pair = getCurrentPair();
            if (pair.Defender == null || pair.Attacker == null)
            {
                XLogger.LogError(LOG_TAG, "startPerformance: 当前演出对(" + _pair + ")未接线,无法开始演出");
                return;
            }
            _didStrike = false;
            _didRecover = false;
            _didResumeAfterReturn = false;
            _attackerAnimationEnded = false;
            _returnHomeCompleted = false;
            _roundDefender = pair.Defender;
            _roundAttacker = pair.Attacker;
            bindAttackerAnimation(_roundAttacker);
            _roundAttacker.PlayAttack(_settings.AttackClipName);
            _phase = SpineBattlePhase.Windup;
            XLogger.LogInfo(LOG_TAG, "startPerformance: 开始一轮战斗演出,pair=" + _pair);
        }

        /// <summary>
        /// 每帧轮询攻击方 clip 时间:仅前摇阶段越过 AttackStartTime 时触发打击节拍;
        /// 定格开始后攻击方动画不再推进,恢复节拍改由表现窗口到时驱动
        /// </summary>
        private void pollBeatThresholds()
        {
            float attackerTime = _roundAttacker.CurrentAnimationTime;
            if (_phase == SpineBattlePhase.Windup && !_didStrike && attackerTime >= _settings.AttackStartTime)
            {
                triggerStrikeBeat();
            }
        }

        /// <summary>
        /// 打击节拍:攻击方跳帧定格 + 受击方 Hit 定格(双定格)+ 瞬时挂载放大 + 组/背景放大 +
        /// 幕布开 + Stage 启动相机晃动,并启动表现窗口计时
        /// </summary>
        private void triggerStrikeBeat()
        {
            _didStrike = true;
            _roundAttacker.JumpFreezeAt(_settings.AttackFreezeFrame);
            _roundDefender.FreezeHitAt(_settings.HitFrameTime);
            // 顺序约束:先挂载演出对,再放大 SpiderGroup,避免演出对叠加组倍率
            _stage.SnapFocusIn(_roundDefender.transform, _roundAttacker.transform);
            _stage.ZoomGroupIn();
            _battleCanvas.gameObject.SetActive(true);
            _stage.StartCameraShake();
            _phase = SpineBattlePhase.Strike;
            startPresentationTimer();
            XLogger.LogInfo(LOG_TAG,
                string.Format("triggerStrikeBeat: 打击节拍触发(双定格),表现窗口={0:F3}s", _settings.PresentationDuration));
        }

        /// <summary>
        /// 恢复节拍:表现窗口结束,执行幕布关闭与 Stage 收尾序列(停止相机晃动、组/背景复原、
        /// 缩小回 Home 并还原);演出对动画保持定格,续播延迟到回位完成(见
        /// handleReturnHomeCompleted),由 Stage 回位完成回调驱动
        /// </summary>
        private void triggerRecoverBeat()
        {
            _didRecover = true;
            stopPresentationTimer();
            _stage.RecoverPresentation(_roundDefender.transform, _roundAttacker.transform, _settings.ReturnDuration);
            _battleCanvas.gameObject.SetActive(false);
            _phase = SpineBattlePhase.Recover;
            XLogger.LogInfo(LOG_TAG, "triggerRecoverBeat: 恢复节拍触发,定格收缩回位,回位完成后续播");
        }

        /// <summary>
        /// 启动表现窗口计时协程:到时且未恢复则触发恢复节拍
        /// </summary>
        private void startPresentationTimer()
        {
            stopPresentationTimer();
            _presentationCoroutine = StartCoroutine(presentationWindowCoroutine());
        }

        /// <summary>
        /// 停止表现窗口计时协程
        /// </summary>
        private void stopPresentationTimer()
        {
            if (_presentationCoroutine != null)
            {
                StopCoroutine(_presentationCoroutine);
                _presentationCoroutine = null;
            }
        }

        /// <summary>
        /// 表现窗口协程:等待 PresentationDuration 后触发恢复节拍(表现窗口结束)
        /// </summary>
        private System.Collections.IEnumerator presentationWindowCoroutine()
        {
            yield return new WaitForSeconds(_settings.PresentationDuration);
            _presentationCoroutine = null;
            if (_phase == SpineBattlePhase.Strike && !_didRecover)
            {
                triggerRecoverBeat();
            }
        }

        /// <summary>
        /// 攻击方动画结束回调:Strike 态动画先结束的极端路径先补恢复节拍;Recover 态记录条件并尝试收尾
        /// </summary>
        private void handleActorAnimationEnded()
        {
            if (_phase == SpineBattlePhase.Ready)
            {
                return;
            }
            if (_phase == SpineBattlePhase.Strike)
            {
                // 双定格下攻击方动画不会自行播完,此处仅兜底极端参数
                triggerRecoverBeat();
            }
            if (_phase != SpineBattlePhase.Recover)
            {
                return;
            }
            _attackerAnimationEnded = true;
            tryFinishRound();
        }

        /// <summary>
        /// Stage 回位完成回调:定格收缩回 Home 完成且还原缓存父级与本地 TRS 后,
        /// 才解除定格让双方自定格帧常速续播,并记录条件尝试收尾(单次守卫)
        /// </summary>
        private void handleReturnHomeCompleted()
        {
            if (_phase != SpineBattlePhase.Recover)
            {
                return;
            }
            if (_didResumeAfterReturn)
            {
                return;
            }
            _didResumeAfterReturn = true;
            _roundAttacker.Resume();
            _roundDefender.Resume();
            _returnHomeCompleted = true;
            tryFinishRound();
        }

        /// <summary>
        /// 收尾双条件:攻击动画结束与回位完成均达成后,双方回 Idle、状态回 Ready
        /// </summary>
        private void tryFinishRound()
        {
            if (!_attackerAnimationEnded || !_returnHomeCompleted)
            {
                return;
            }
            _roundDefender.PlayIdle();
            _roundAttacker.PlayIdle();
            _phase = SpineBattlePhase.Ready;
            XLogger.LogInfo(LOG_TAG, "tryFinishRound: 演出结束,双方回 Idle,状态回 Ready");
        }

        /// <summary>
        /// 绑定攻击方动画结束事件,换对时先解绑旧攻击方
        /// </summary>
        private void bindAttackerAnimation(SpineActor attacker)
        {
            if (attacker == null)
            {
                XLogger.LogError(LOG_TAG, "bindAttackerAnimation: attacker 为空,无法订阅动画结束事件");
                return;
            }
            if (_boundAttacker == attacker)
            {
                return;
            }
            unbindAttackerAnimation();
            _boundAttacker = attacker;
            _boundAttacker.OnAnimationEnded += handleActorAnimationEnded;
        }

        /// <summary>
        /// 解绑攻击方动画结束事件,防止生命周期泄漏
        /// </summary>
        private void unbindAttackerAnimation()
        {
            if (_boundAttacker != null)
            {
                _boundAttacker.OnAnimationEnded -= handleActorAnimationEnded;
                _boundAttacker = null;
            }
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
        /// 调试按键处理:按 T 打印当前演出对与双方动画时间
        /// </summary>
        private void handleDebugKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }
            if (!keyboard.tKey.wasPressedThisFrame)
            {
                return;
            }
            PairSetup pair = getCurrentPair();
            if (pair.Defender == null || pair.Attacker == null)
            {
                XLogger.LogWarning(LOG_TAG, "handleDebugKeys: 当前演出对未接线,无法打印调试信息");
                return;
            }
            XLogger.LogInfo(LOG_TAG,
                string.Format("handleDebugKeys: phase={0}, pair={1}, {2}.AnimationTime={3:F3}, {4}.AnimationTime={5:F3}",
                    _phase, _pair, pair.Defender.name, pair.Defender.CurrentAnimationTime,
                    pair.Attacker.name, pair.Attacker.CurrentAnimationTime));
        }
    }

    /// <summary>
    /// 一组演出对的角色引用配置(Defender 受击 / Attacker 攻击)
    /// </summary>
    [System.Serializable]
    public class PairSetup
    {
        /// <summary>
        /// 受击方 SpineActor
        /// </summary>
        public SpineActor Defender;

        /// <summary>
        /// 攻击方 SpineActor
        /// </summary>
        public SpineActor Attacker;
    }

    /// <summary>
    /// 战斗演出的阶段状态
    /// </summary>
    public enum SpineBattlePhase
    {
        /// <summary>待机:可接受 B 键触发新一轮演出</summary>
        Ready,

        /// <summary>前摇:攻击方蓄力,等待打击起点</summary>
        Windup,

        /// <summary>打击:双定格 + 瞬时聚焦 + 表现窗口(相机晃动),等待窗口到时</summary>
        Strike,

        /// <summary>恢复:定格收缩回位,回位完成后续播,等待动画结束与回位完成</summary>
        Recover,
    }

    /// <summary>
    /// 演出对枚举:仅作"取哪一组角色引用"的开关,具体角色在 Inspector 接线
    /// </summary>
    public enum SpineBattlePair
    {
        /// <summary>演出对 AB:SpiderA 受击 / SpiderB 攻击(默认)</summary>
        Pair_AB,

        /// <summary>演出对 CD:SpiderC 受击 / SpiderD 攻击(预留)</summary>
        Pair_CD,
    }
}
