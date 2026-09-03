using System;
using System.Collections;
using ShowX.Utils;
using UnityEngine;

namespace SpineTest.Battle
{
    /// <summary>
    /// 战斗舞台:负责四只蜘蛛启动摆位到各自 Home 锚点、演出对(受击方/攻击方)瞬时挂载聚焦与放大、
    /// 表现窗口内演示相机晃动(冲击表现)、SpiderGroup/background 组缩放、缩小回位与整组还原。
    /// 只做变换执行与插值推进,不感知 Spine 动画时间,不修改全局时间刻度;
    /// 数值参数统一读取 SpineBattleSettings(单一配置源)。
    /// </summary>
    public class BattleStage : MonoBehaviour
    {
        // ==================== 常量 ====================

        /// <summary>
        /// 日志标签
        /// </summary>
        private const string LOG_TAG = "BattleStage";

        // ==================== 序列化字段 ====================

        [Header("配置资产")]
        [Tooltip("演出参数单一配置源(节拍/窗口/回位/倍率/相机晃动等)")]
        [SerializeField] private SpineBattleSettings _settings;

        [Header("Home 摆位映射(SpiderA~D ↔ HomeAnchorA~D)")]
        [Tooltip("四只蜘蛛与其 Home 锚点的映射,启动时按此将世界位置对齐锚点")]
        [SerializeField] private HomeMapping[] _homeMappings;

        [Header("演出锚点(受击位 / 攻击位)")]
        [Tooltip("受击位特写锚点(场景 FocusAnchorA)")]
        [SerializeField] private Transform _defenderFocusAnchor;

        [Tooltip("攻击位特写锚点(场景 FocusAnchorB)")]
        [SerializeField] private Transform _attackerFocusAnchor;

        [Tooltip("演示相机(场景 BattleDemo Camera),表现窗口内晃动、结束后还原基准")]
        [SerializeField] private Transform _demoCamera;

        [Header("组与背景缩放")]
        [Tooltip("SpiderGroup,打击节拍时轻微放大、恢复节拍时复原")]
        [SerializeField] private Transform _spiderGroup;

        [Tooltip("background(BackUI 幕布下的背景),随 SpiderGroup 同步放大/复原")]
        [SerializeField] private Transform _background;

        // ==================== 私有字段 ====================

        /// <summary>
        /// 是否正在演出(演出对已挂载、未还原)
        /// </summary>
        private bool _isPerforming;

        /// <summary>
        /// SpiderGroup/background 是否处于放大态
        /// </summary>
        private bool _isGroupZoomed;

        /// <summary>
        /// 放大前 SpiderGroup 的基准缩放,复原时精确还原
        /// </summary>
        private Vector3 _groupBaseScale;

        /// <summary>
        /// 放大前 background 的基准缩放,复原时精确还原
        /// </summary>
        private Vector3 _backgroundBaseScale;

        /// <summary>
        /// 当前演出的受击方 Transform
        /// </summary>
        private Transform _activeDefender;

        /// <summary>
        /// 当前演出的攻击方 Transform
        /// </summary>
        private Transform _activeAttacker;

        /// <summary>
        /// 受击方挂载前的父级与本地 TRS 快照
        /// </summary>
        private PerformerSnapshot _defenderSnapshot;

        /// <summary>
        /// 攻击方挂载前的父级与本地 TRS 快照
        /// </summary>
        private PerformerSnapshot _attackerSnapshot;

        /// <summary>
        /// 正在执行的回位插值协程
        /// </summary>
        private Coroutine _movementCoroutine;

        /// <summary>
        /// 演出前记录的相机本地基准位置
        /// </summary>
        private Vector3 _cameraBaseLocalPosition;

        /// <summary>
        /// 演出前记录的相机本地基准旋转
        /// </summary>
        private Quaternion _cameraBaseLocalRotation;

        /// <summary>
        /// 相机是否正在晃动
        /// </summary>
        private bool _isCameraShaking;

        /// <summary>
        /// 正在执行的相机晃动协程
        /// </summary>
        private Coroutine _cameraShakeCoroutine;

        // ==================== 公共属性与事件 ====================

        /// <summary>
        /// 回位补间完成并已整组还原缓存变换时触发,供 Director 收尾双条件使用
        /// </summary>
        public event Action OnReturnHomeCompleted;

        // ==================== Unity 生命周期 ====================

        private void Start()
        {
            if (!tryValidateRefs())
            {
                return;
            }
            PlaceHome();
            XLogger.LogInfo(LOG_TAG, "Start: 舞台初始化完成,四蜘蛛已对齐 Home 锚点");
        }

        // ==================== 公共方法 ====================

        /// <summary>
        /// 启动摆位:将四只蜘蛛的世界位置分别对齐各自 Home 锚点(不改父级/缩放/旋转)
        /// </summary>
        public void PlaceHome()
        {
            for (int i = 0; i < _homeMappings.Length; i++)
            {
                HomeMapping mapping = _homeMappings[i];
                if (mapping.Actor == null || mapping.HomeAnchor == null)
                {
                    XLogger.LogError(LOG_TAG, "PlaceHome: 第 " + i + " 组摆位映射存在空引用,请在 Inspector 接线");
                    continue;
                }
                mapping.Actor.position = mapping.HomeAnchor.position;
            }
        }

        /// <summary>
        /// 瞬时聚焦挂载:缓存演出对当前父级与本地 TRS,随后挂到各自 FocusAnchor 并贴齐锚点、
        /// 按配置的焦点倍率乘性放大,单帧完成无补间
        /// </summary>
        public void SnapFocusIn(Transform defender, Transform attacker)
        {
            if (defender == null || attacker == null)
            {
                XLogger.LogError(LOG_TAG, "SnapFocusIn: defender / attacker 为空,无法挂载演出对");
                return;
            }
            if (!tryValidateRefs())
            {
                return;
            }
            if (_isPerforming)
            {
                XLogger.LogWarning(LOG_TAG, "SnapFocusIn: 上一轮演出尚未回位还原,忽略重复挂载");
                return;
            }
            _isPerforming = true;
            _activeDefender = defender;
            _activeAttacker = attacker;
            snapPerformer(defender, _defenderFocusAnchor, ref _defenderSnapshot);
            snapPerformer(attacker, _attackerFocusAnchor, ref _attackerSnapshot);
            XLogger.LogInfo(LOG_TAG, "SnapFocusIn: 演出对已瞬时挂载到 FocusAnchor 并按焦点倍率放大");
        }

        /// <summary>
        /// 放大 SpiderGroup/background 至配置的组倍率(必须先挂载演出对,避免双重放大)
        /// </summary>
        public void ZoomGroupIn()
        {
            if (!tryValidateRefs())
            {
                return;
            }
            if (_isGroupZoomed)
            {
                return;
            }
            _groupBaseScale = _spiderGroup.localScale;
            _backgroundBaseScale = _background.localScale;
            Vector3 groupZoomScale = Vector3.one * _settings.GroupScaleMultiplier;
            _spiderGroup.localScale = Vector3.Scale(_groupBaseScale, groupZoomScale);
            _background.localScale = Vector3.Scale(_backgroundBaseScale, groupZoomScale);
            _isGroupZoomed = true;
        }

        /// <summary>
        /// 复原 SpiderGroup/background 至放大前基准缩放
        /// </summary>
        public void ZoomGroupOut()
        {
            if (!_isGroupZoomed)
            {
                return;
            }
            _spiderGroup.localScale = _groupBaseScale;
            _background.localScale = _backgroundBaseScale;
            _isGroupZoomed = false;
        }

        /// <summary>
        /// 启动表现窗口的相机晃动:以当前相机本地位姿为基准,按配置幅度/速度做噪声晃动,
        /// 直到 StopCameraShake() 停止并还原
        /// </summary>
        public void StartCameraShake()
        {
            if (!tryValidateRefs())
            {
                return;
            }
            if (_isCameraShaking)
            {
                return;
            }
            _cameraBaseLocalPosition = _demoCamera.localPosition;
            _cameraBaseLocalRotation = _demoCamera.localRotation;
            _isCameraShaking = true;
            _cameraShakeCoroutine = StartCoroutine(cameraShakeCoroutine());
        }

        /// <summary>
        /// 停止相机晃动并精确还原演出前相机本地位姿
        /// </summary>
        public void StopCameraShake()
        {
            if (_cameraShakeCoroutine != null)
            {
                StopCoroutine(_cameraShakeCoroutine);
                _cameraShakeCoroutine = null;
            }
            if (_isCameraShaking)
            {
                _isCameraShaking = false;
                restoreCameraBase();
            }
        }

        /// <summary>
        /// 恢复节拍触发的舞台收尾序列:停止相机晃动并还原 → 组/背景复原 →
        /// 缩小回位并还原缓存父级与本地 TRS(上抛回位完成回调)
        /// </summary>
        public void RecoverPresentation(Transform defender, Transform attacker, float returnDuration)
        {
            if (defender != _activeDefender || attacker != _activeAttacker)
            {
                XLogger.LogError(LOG_TAG, "RecoverPresentation: 传入角色与当前演出对不一致,忽略");
                return;
            }
            if (!_isPerforming)
            {
                XLogger.LogError(LOG_TAG, "RecoverPresentation: 演出对尚未挂载,忽略恢复请求");
                return;
            }
            startMovement(recoverPresentationCoroutine(returnDuration));
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 校验配置资产、Home 映射、演出锚点、相机与组/背景引用是否齐全
        /// </summary>
        private bool tryValidateRefs()
        {
            if (_settings == null)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: _settings 为空,请在 Inspector 挂载 SpineBattleCinematicSettings 资产");
                return false;
            }
            if (_homeMappings == null || _homeMappings.Length == 0)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: _homeMappings 未配置任何摆位映射");
                return false;
            }
            for (int i = 0; i < _homeMappings.Length; i++)
            {
                HomeMapping mapping = _homeMappings[i];
                if (mapping.Actor == null || mapping.HomeAnchor == null)
                {
                    XLogger.LogError(LOG_TAG, "tryValidateRefs: 第 " + i + " 组 Home 摆位映射存在空引用,请在 Inspector 接线");
                    return false;
                }
            }
            if (_defenderFocusAnchor == null || _attackerFocusAnchor == null || _demoCamera == null)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: FocusAnchor / 演示相机存在为空,请在 Inspector 接线");
                return false;
            }
            if (_spiderGroup == null || _background == null)
            {
                XLogger.LogError(LOG_TAG, "tryValidateRefs: _spiderGroup / _background 为空,请在 Inspector 接线");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 缓存单个演出角色的父级与本地 TRS 快照,并挂载到指定 FocusAnchor 贴齐锚点后乘性放大
        /// </summary>
        private void snapPerformer(Transform performer, Transform focusAnchor, ref PerformerSnapshot snapshot)
        {
            if (performer == null || focusAnchor == null)
            {
                XLogger.LogError(LOG_TAG, "snapPerformer: 传入参数为空,忽略");
                return;
            }

            if (snapshot == null)
            {
                snapshot = new PerformerSnapshot();
            }

            snapshot.Parent = performer.parent;
            snapshot.LocalPosition = performer.localPosition;
            snapshot.LocalRotation = performer.localRotation;
            snapshot.LocalScale = performer.localScale;
            snapshot.FocusAnchor = focusAnchor;
            performer.SetParent(focusAnchor, true);
            // 本地位置归零以贴齐锚点原点(锚点无旋转缩放时即锚点世界位置)
            performer.localPosition = Vector3.zero;
            performer.localScale = Vector3.Scale(performer.localScale, Vector3.one * _settings.FocusScaleMultiplier);
        }

        /// <summary>
        /// 打断旧插值并启动一段新的回位协程
        /// </summary>
        private void startMovement(IEnumerator movementRoutine)
        {
            if (_movementCoroutine != null)
            {
                StopCoroutine(_movementCoroutine);
                _movementCoroutine = null;
            }
            _movementCoroutine = StartCoroutine(movementRoutine);
        }

        /// <summary>
        /// 相机晃动协程:以演出前基准为中心,按配置幅度/速度对本地位置(可含旋转)做噪声晃动
        /// </summary>
        private IEnumerator cameraShakeCoroutine()
        {
            float amplitude = _settings.CameraShakeAmplitude;
            float speed = _settings.CameraShakeSpeed;
            float rotationAmplitude = _settings.CameraShakeRotationAmplitude;
            while (_isCameraShaking)
            {
                float timeSeed = Time.time * speed;
                // float offsetX = (Mathf.PerlinNoise(timeSeed, 0f) - 0.5f) * amplitude * 2f;
                float offsetY = (Mathf.PerlinNoise(0f, timeSeed) - 0.5f) * amplitude * 2f;
                _demoCamera.localPosition = _cameraBaseLocalPosition + new Vector3(0, offsetY, 0f);
                if (rotationAmplitude > 0f)
                {
                    float angleZ = (Mathf.PerlinNoise(timeSeed, timeSeed + 13f) - 0.5f) * rotationAmplitude * 2f;
                    _demoCamera.localRotation = _cameraBaseLocalRotation * Quaternion.Euler(0f, 0f, angleZ);
                }
                yield return null;
            }
            restoreCameraBase();
        }

        /// <summary>
        /// 将相机本地位姿还原到演出前基准
        /// </summary>
        private void restoreCameraBase()
        {
            if (_demoCamera == null)
            {
                return;
            }
            _demoCamera.localPosition = _cameraBaseLocalPosition;
            _demoCamera.localRotation = _cameraBaseLocalRotation;
        }

        /// <summary>
        /// 恢复序列协程:停止相机晃动并还原 → 组/背景复原 → 缩小回 Home 原位
        /// (还原父级与本地 TRS)并上抛回位完成回调
        /// </summary>
        private IEnumerator recoverPresentationCoroutine(float returnDuration)
        {
            StopCameraShake();
            ZoomGroupOut();
            Vector3 homeDefenderLocal = _defenderSnapshot.FocusAnchor.InverseTransformPoint(worldPositionFromSnapshot(_defenderSnapshot));
            Vector3 homeAttackerLocal = _attackerSnapshot.FocusAnchor.InverseTransformPoint(worldPositionFromSnapshot(_attackerSnapshot));
            yield return StartCoroutine(returnHomeCoroutine(returnDuration, homeDefenderLocal, homeAttackerLocal));
        }

        /// <summary>
        /// 由快照推导演出前 Home 世界位置,父级为空时直接取本地位置
        /// </summary>
        private Vector3 worldPositionFromSnapshot(PerformerSnapshot snapshot)
        {
            if (snapshot.Parent == null)
            {
                return snapshot.LocalPosition;
            }
            return snapshot.Parent.TransformPoint(snapshot.LocalPosition);
        }

        /// <summary>
        /// 缩小回位协程:位置由当前点插值回 Home 原位,缩放由焦点倍率回落至缓存基准;
        /// 结束后整组还原父级与本地 TRS 并上抛回位完成回调
        /// </summary>
        private IEnumerator returnHomeCoroutine(float duration, Vector3 homeDefenderLocal, Vector3 homeAttackerLocal)
        {
            Vector3 startDefenderLocal = _activeDefender.localPosition;
            Vector3 startAttackerLocal = _activeAttacker.localPosition;
            Vector3 startDefenderScale = _activeDefender.localScale;
            Vector3 startAttackerScale = _activeAttacker.localScale;
            if (duration <= 0f)
            {
                applyReturnHomeEnd(homeDefenderLocal, homeAttackerLocal);
                yield break;
            }
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                _activeDefender.localPosition = Vector3.LerpUnclamped(startDefenderLocal, homeDefenderLocal, progress);
                _activeAttacker.localPosition = Vector3.LerpUnclamped(startAttackerLocal, homeAttackerLocal, progress);
                _activeDefender.localScale = Vector3.LerpUnclamped(startDefenderScale, _defenderSnapshot.LocalScale, progress);
                _activeAttacker.localScale = Vector3.LerpUnclamped(startAttackerScale, _attackerSnapshot.LocalScale, progress);
                yield return null;
            }
            applyReturnHomeEnd(homeDefenderLocal, homeAttackerLocal);
        }

        /// <summary>
        /// 回位收尾:贴齐 Home 目标后整组还原缓存父级与本地 TRS,复位演出状态并上抛回位完成回调
        /// </summary>
        private void applyReturnHomeEnd(Vector3 homeDefenderLocal, Vector3 homeAttackerLocal)
        {
            _activeDefender.localPosition = homeDefenderLocal;
            _activeAttacker.localPosition = homeAttackerLocal;
            restorePerformer(_activeDefender, _defenderSnapshot);
            restorePerformer(_activeAttacker, _attackerSnapshot);
            _isPerforming = false;
            finishMovement();
            clearRoundReferences();
            raiseReturnHomeCompleted();
        }

        /// <summary>
        /// 清空本轮演出引用与快照,保证下一轮从无历史引用状态开始
        /// </summary>
        private void clearRoundReferences()
        {
            _activeDefender = null;
            _activeAttacker = null;
            _defenderSnapshot = null;
            _attackerSnapshot = null;
        }

        /// <summary>
        /// 还原单个演出角色的父级与本地 TRS 至演出前快照
        /// </summary>
        private void restorePerformer(Transform performer, PerformerSnapshot snapshot)
        {
            if (performer == null)
            {
                return;
            }
            performer.SetParent(snapshot.Parent, false);
            performer.localPosition = snapshot.LocalPosition;
            performer.localRotation = snapshot.LocalRotation;
            performer.localScale = snapshot.LocalScale;
        }

        /// <summary>
        /// 清理协程引用并标记插值结束
        /// </summary>
        private void finishMovement()
        {
            _movementCoroutine = null;
        }

        /// <summary>
        /// 上抛回位完成回调,先取局部引用避免回调中反订阅问题
        /// </summary>
        private void raiseReturnHomeCompleted()
        {
            Action handler = OnReturnHomeCompleted;
            if (handler != null)
            {
                handler();
            }
        }
    }

    /// <summary>
    /// 角色与其 Home 锚点的摆位映射,Inspector 中按 SpiderA~D ↔ HomeAnchorA~D 配置
    /// </summary>
    [Serializable]
    public class HomeMapping
    {
        /// <summary>
        /// 角色 Transform(如 SpiderA)
        /// </summary>
        public Transform Actor;

        /// <summary>
        /// 对应的 Home 锚点(如 HomeAnchorA)
        /// </summary>
        public Transform HomeAnchor;
    }

    /// <summary>
    /// 演出角色挂载前的父级与本地 TRS 快照,用于回位时整组精确还原
    /// </summary>
    [Serializable]
    public class PerformerSnapshot
    {
        /// <summary>
        /// 演出前的父级 Transform
        /// </summary>
        public Transform Parent;

        /// <summary>
        /// 演出前的本地位置
        /// </summary>
        public Vector3 LocalPosition;

        /// <summary>
        /// 演出前的本地旋转
        /// </summary>
        public Quaternion LocalRotation;

        /// <summary>
        /// 演出前的本地缩放
        /// </summary>
        public Vector3 LocalScale;

        /// <summary>
        /// 挂载的 FocusAnchor
        /// </summary>
        public Transform FocusAnchor;
    }
}
