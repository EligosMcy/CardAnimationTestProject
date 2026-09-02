using System.Collections;
using ShowX.Utils;
using UnityEngine;

namespace SpineTest.Battle
{
    /// <summary>
    /// 双人对战舞台:负责 A / B 两个角色在 Home / Focus 两组锚点之间的位置插值,
    /// 以及"乘性" localScale 缩放补间,实现不移动相机的拉近放大与回位。
    /// 只做补间执行,不感知 Spine 动画时间,也不修改全局时间刻度。
    /// </summary>
    public class BattleStage : MonoBehaviour
    {
        // ==================== 常量 ====================

        /// <summary>
        /// 日志标签
        /// </summary>
        private const string LOG_TAG = "BattleStage";

        // ==================== 序列化字段 ====================

        [Header("对战角色")]
        [Tooltip("受击方 A 的 Transform")]
        [SerializeField] private Transform _actorA;

        [Tooltip("攻击方 B 的 Transform")]
        [SerializeField] private Transform _actorB;

        [Header("锚点")]
        [Tooltip("A 待机位置锚点")]
        [SerializeField] private Transform _homeAnchorA;

        [Tooltip("B 待机位置锚点")]
        [SerializeField] private Transform _homeAnchorB;

        [Tooltip("A 聚焦(拉近)位置锚点")]
        [SerializeField] private Transform _focusAnchorA;

        [Tooltip("B 聚焦(拉近)位置锚点")]
        [SerializeField] private Transform _focusAnchorB;

        [Header("聚焦补间")]
        [Tooltip("聚焦放大倍率,乘性作用于 localScale,保留镜像角色的负 X 缩放")]
        [SerializeField] private float _focusScaleMultiplier = 1.5f;

        [Tooltip("单程补间时长(秒)")]
        [SerializeField] private float _tweenDuration = 0.4f;

        [Tooltip("补间缓动曲线,时间轴 0~1 映射到插值进度 0~1")]
        [SerializeField] private AnimationCurve _tweenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ==================== 私有字段 ====================

        /// <summary>
        /// A 的基准缩放(待机姿态),放大/回位都以此乘性变换
        /// </summary>
        private Vector3 _baseScaleA;

        /// <summary>
        /// B 的基准缩放(待机姿态),放大/回位都以此乘性变换
        /// </summary>
        private Vector3 _baseScaleB;

        /// <summary>
        /// 当前正在执行的补间协程,用于打断上一次未完成的补间
        /// </summary>
        private Coroutine _activeTween;

        // ==================== Unity 生命周期 ====================

        private void Start()
        {
            if (!tryValidateRefs())
            {
                return;
            }
            _baseScaleA = _actorA.localScale;
            _baseScaleB = _actorB.localScale;
        }

        // ==================== 公共方法 ====================

        /// <summary>
        /// 双人从当前状态补间到 Focus 锚点并按倍率放大
        /// </summary>
        public void FocusIn()
        {
            if (!tryValidateRefs())
            {
                return;
            }
            Vector3 focusScale = Vector3.one * _focusScaleMultiplier;
            Vector3 targetScaleA = Vector3.Scale(_baseScaleA, focusScale);
            Vector3 targetScaleB = Vector3.Scale(_baseScaleB, focusScale);
            startTween(_focusAnchorA.position, _focusAnchorB.position, targetScaleA, targetScaleB);
        }

        /// <summary>
        /// 双人从当前状态补间回 Home 锚点并恢复基准缩放
        /// </summary>
        public void FocusOut()
        {
            if (!tryValidateRefs())
            {
                return;
            }
            startTween(_homeAnchorA.position, _homeAnchorB.position, _baseScaleA, _baseScaleB);
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 校验角色与锚点引用是否齐全,缺失时输出错误并中断流程
        /// </summary>
        private bool tryValidateRefs()
        {
            if (_actorA == null || _actorB == null)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: _actorA / _actorB 为空,请在 Inspector 赋值");
                return false;
            }
            if (_homeAnchorA == null || _homeAnchorB == null || _focusAnchorA == null || _focusAnchorB == null)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: 四个锚点存在为空,请在 Inspector 赋值");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 打断旧补间并启动一段新的双人补间协程
        /// </summary>
        private void startTween(Vector3 targetPosA, Vector3 targetPosB, Vector3 targetScaleA, Vector3 targetScaleB)
        {
            if (_activeTween != null)
            {
                StopCoroutine(_activeTween);
                _activeTween = null;
            }
            _activeTween = StartCoroutine(tweenCoroutine(targetPosA, targetPosB, targetScaleA, targetScaleB));
        }

        /// <summary>
        /// 双人补间协程:用真实时间推进位置与缩放插值
        /// </summary>
        private IEnumerator tweenCoroutine(Vector3 targetPosA, Vector3 targetPosB, Vector3 targetScaleA, Vector3 targetScaleB)
        {
            if (_tweenDuration <= 0f)
            {
                applyTargets(targetPosA, targetPosB, targetScaleA, targetScaleB);
                _activeTween = null;
                yield break;
            }
            Vector3 startPosA = _actorA.position;
            Vector3 startPosB = _actorB.position;
            Vector3 startScaleA = _actorA.localScale;
            Vector3 startScaleB = _actorB.localScale;
            float elapsedTime = 0f;
            while (elapsedTime < _tweenDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / _tweenDuration);
                float curveValue = _tweenCurve != null ? _tweenCurve.Evaluate(normalizedTime) : normalizedTime;
                _actorA.position = Vector3.LerpUnclamped(startPosA, targetPosA, curveValue);
                _actorB.position = Vector3.LerpUnclamped(startPosB, targetPosB, curveValue);
                _actorA.localScale = Vector3.LerpUnclamped(startScaleA, targetScaleA, curveValue);
                _actorB.localScale = Vector3.LerpUnclamped(startScaleB, targetScaleB, curveValue);
                yield return null;
            }
            // 收尾对齐精确目标值
            applyTargets(targetPosA, targetPosB, targetScaleA, targetScaleB);
            _activeTween = null;
        }

        /// <summary>
        /// 将双人位置与缩放直接设置到目标值
        /// </summary>
        private void applyTargets(Vector3 targetPosA, Vector3 targetPosB, Vector3 targetScaleA, Vector3 targetScaleB)
        {
            _actorA.position = targetPosA;
            _actorB.position = targetPosB;
            _actorA.localScale = targetScaleA;
            _actorB.localScale = targetScaleB;
        }
    }
}
