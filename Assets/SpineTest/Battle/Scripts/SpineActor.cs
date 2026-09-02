using System;
using ShowX.Utils;
using Spine;
using Spine.Unity;
using UnityEngine;
using AnimationState = Spine.AnimationState;

namespace SpineTest.Battle
{
    /// <summary>
    /// 单个 Spine 角色的动画驱动,对上层隔离全部 Spine API。
    /// 提供待机 / 攻击 / 受击定格与恢复 / 轨道变速 / 当前动画时间读取;
    /// 非循环动画(攻击、受击)自然播完后自动回到待机循环,并对外抛出结束事件。
    /// </summary>
    public class SpineActor : MonoBehaviour
    {
        // ==================== 常量 ====================

        /// <summary>
        /// 日志标签
        /// </summary>
        private const string LOG_TAG = "SpineActor";

        /// <summary>
        /// Spine 轨道索引,本组件固定使用主轨道
        /// </summary>
        private const int MAIN_TRACK_INDEX = 0;

        /// <summary>
        /// 无当前动画时读取时间返回的默认值
        /// </summary>
        private const float NO_ENTRY_TIME = 0f;

        /// <summary>
        /// 正常速度的时间刻度
        /// </summary>
        private const float NORMAL_TIME_SCALE = 1f;

        /// <summary>
        /// 零时间刻度,用于受击冻结
        /// </summary>
        private const float FROZEN_TIME_SCALE = 0f;

        // ==================== 序列化字段 ====================

        [Header("Spine 组件")]
        [Tooltip("角色身上的 SkeletonAnimation 组件")]
        [SerializeField] private SkeletonAnimation _skeletonAnimation;

        [Header("动画 Clip 名(注意资产拼写:Idie)")]
        [Tooltip("待机动画名(循环),Spine 资产实际拼写为 Idie")]
        [SerializeField] private string _idleClipName = "Idie";

        [Tooltip("受击动画名(非循环)")]
        [SerializeField] private string _hitClipName = "Hit";

        [Tooltip("默认攻击动画名,PlayAttack 传入空名时的兜底")]
        [SerializeField] private string _attackClipName = "Attack1";

        [Header("过渡")]
        [Tooltip("受击定格时使用的混合时长,置 0 可让目标帧姿态精确无过渡闪现")]
        [SerializeField] private float _freezeMixDuration = 0f;

        // ==================== 私有字段 ====================

        /// <summary>
        /// 缓存的 Spine 动画状态,负责轨道播放与事件
        /// </summary>
        private AnimationState _animationState;

        /// <summary>
        /// 是否已订阅 Complete 事件,避免重复订阅
        /// </summary>
        private bool _isSubscribed;

        // ==================== 公共属性与事件 ====================

        /// <summary>
        /// 当前主轨道动画的 clip 时间(计入轨道 TimeScale),无轨道时为 0
        /// </summary>
        public float CurrentAnimationTime
        {
            get
            {
                TrackEntry entry = getCurrentTrackEntry();
                if (entry == null)
                {
                    return NO_ENTRY_TIME;
                }
                return entry.AnimationTime;
            }
        }

        /// <summary>
        /// 非循环动画(如攻击 / 受击)自然播完时触发
        /// </summary>
        public event Action OnAnimationEnded;

        // ==================== Unity 生命周期 ====================

        private void Start()
        {
            if (!tryInitState())
            {
                return;
            }
            subscribeCompleteEvent();
            // 开场自动进入循环待机,避免停留在 Setup Pose
            PlayIdle();
        }

        private void OnDestroy()
        {
            unsubscribeCompleteEvent();
        }

        // ==================== 公共方法 ====================

        /// <summary>
        /// 播放循环待机动画
        /// </summary>
        public void PlayIdle()
        {
            playClip(_idleClipName, true);
        }

        /// <summary>
        /// 播放一次攻击动画(非循环),名称传空时回退到默认攻击动画
        /// </summary>
        public void PlayAttack(string attackClipName)
        {
            string clipName = string.IsNullOrEmpty(attackClipName) ? _attackClipName : attackClipName;
            playClip(clipName, false);
        }

        /// <summary>
        /// 播放受击动画并冻结在指定受击帧,使角色恒定显示该帧姿态
        /// </summary>
        public void FreezeHitAt(float hitFrameTime)
        {
            if (!tryInitState())
            {
                return;
            }
            if (string.IsNullOrEmpty(_hitClipName))
            {
                XLogger.LogError(LOG_TAG, "FreezeHitAt: _hitClipName 为空,请在 Inspector 配置受击动画名");
                return;
            }
            TrackEntry entry = _animationState.SetAnimation(MAIN_TRACK_INDEX, _hitClipName, false);
            if (entry == null)
            {
                XLogger.LogError(LOG_TAG, "FreezeHitAt: SetAnimation 返回空,受击动画未生效");
                return;
            }
            entry.SetMixDuration(_freezeMixDuration, 0f);
            entry.TrackTime = hitFrameTime;
            entry.TimeScale = FROZEN_TIME_SCALE;
            // 立即采样目标帧姿态,避免本帧闪现初始帧
            _skeletonAnimation.Update(0f);
        }

        /// <summary>
        /// 解除受击冻结,以常速从被冻结的帧继续播放剩余动画
        /// </summary>
        public void Resume()
        {
            TrackEntry entry = getCurrentTrackEntry();
            if (entry == null)
            {
                XLogger.LogWarning(LOG_TAG, "Resume: 主轨道无当前动画,忽略");
                return;
            }
            entry.TimeScale = NORMAL_TIME_SCALE;
        }

        /// <summary>
        /// 设置当前主轨道动画的时间刻度(慢放 / 恢复常速)
        /// </summary>
        public void SetTimeScale(float timeScale)
        {
            if (timeScale < 0f)
            {
                XLogger.LogError(LOG_TAG, "SetTimeScale: 时间刻度不允许为负值,timeScale=" + timeScale);
                return;
            }
            TrackEntry entry = getCurrentTrackEntry();
            if (entry == null)
            {
                XLogger.LogWarning(LOG_TAG, "SetTimeScale: 主轨道无当前动画,忽略");
                return;
            }
            entry.TimeScale = timeScale;
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 校验并缓存 Spine 状态对象,失败时输出错误并中断流程
        /// </summary>
        private bool tryInitState()
        {
            if (_skeletonAnimation == null)
            {
                XLogger.LogError(LOG_TAG, "tryInitState: _skeletonAnimation 为空,请在 Inspector 赋值");
                return false;
            }
            if (_animationState == null)
            {
                _animationState = _skeletonAnimation.AnimationState;
            }
            if (_animationState == null)
            {
                XLogger.LogError(LOG_TAG, "tryInitState: AnimationState 获取失败,SkeletonAnimation 未初始化");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 订阅 Spine 动画状态完成事件,仅在非循环动画播完时响应
        /// </summary>
        private void subscribeCompleteEvent()
        {
            if (_isSubscribed)
            {
                return;
            }
            _animationState.Complete += handleAnimationComplete;
            _isSubscribed = true;
        }

        /// <summary>
        /// 反订阅完成事件,防止生命周期泄漏
        /// </summary>
        private void unsubscribeCompleteEvent()
        {
            if (!_isSubscribed)
            {
                return;
            }
            if (_animationState != null)
            {
                _animationState.Complete -= handleAnimationComplete;
            }
            _isSubscribed = false;
        }

        /// <summary>
        /// 非循环动画播完回调:自动回到待机循环并对外抛出结束事件
        /// </summary>
        private void handleAnimationComplete(TrackEntry trackEntry)
        {
            if (trackEntry == null)
            {
                return;
            }
            if (trackEntry.Loop)
            {
                return;
            }
            PlayIdle();
            raiseAnimationEnded();
        }

        /// <summary>
        /// 播放指定动画到主轨道
        /// </summary>
        private void playClip(string clipName, bool isLoop)
        {
            if (!tryInitState())
            {
                return;
            }
            if (string.IsNullOrEmpty(clipName))
            {
                XLogger.LogError(LOG_TAG, "playClip: clipName 为空,无法播放动画");
                return;
            }
            _animationState.SetAnimation(MAIN_TRACK_INDEX, clipName, isLoop);
        }

        /// <summary>
        /// 获取当前主轨道条目,未初始化或轨道为空时返回空
        /// </summary>
        private TrackEntry getCurrentTrackEntry()
        {
            if (_animationState == null)
            {
                return null;
            }
            ExposedList<TrackEntry> tracks = _animationState.Tracks;
            if (tracks == null || MAIN_TRACK_INDEX >= tracks.Count)
            {
                return null;
            }
            return tracks.Items[MAIN_TRACK_INDEX];
        }

        /// <summary>
        /// 抛出不带参数的动画结束事件
        /// </summary>
        private void raiseAnimationEnded()
        {
            Action handler = OnAnimationEnded;
            if (handler != null)
            {
                handler();
            }
        }
    }
}
