using System.Collections.Generic;
using NUnit.Framework;
using ShowX.Utils;
using Spine;
using Spine.Unity;
using UnityEngine;
using AnimationState = Spine.AnimationState;

namespace SpineTest
{
    public class DefaultSpine : MonoBehaviour
    {

        [SerializeField]
        private SkeletonRenderer _skeletonRenderer;

        [SerializeField]
        private SkeletonAnimation _skeletonAnimation;

        private Skeleton _rendererSkeleton;

        private Skeleton _animationSkeleton;

        private AnimationState _animationState;

        [Space(20)]
        [SpineAnimation]
        [SerializeField]
        public string _animationProperties;



        private void Start()
        {
            setSkin();
            animationParameter();
            skeletonRenderUpdateCallback();
            setAnimationState();
            animationStateCallback();
        }

        private void setSkin()
        {
            _skeletonRenderer.Skeleton.SetSkin("default");
        }

        private void animationParameter()
        {
            _rendererSkeleton = _skeletonRenderer.Skeleton;

            _animationSkeleton = _skeletonAnimation.Skeleton;

            _animationState = _skeletonAnimation.AnimationState;

            XLogger.LogInfo("Default", $"{_animationState} , {_rendererSkeleton == _animationSkeleton}");
        }


        private void skeletonRenderUpdateCallback()
        {
            _skeletonAnimation.BeforeApply += _skeletonAnimation_BeforeApply;
            _skeletonAnimation.UpdateComplete += _skeletonAnimation_UpdateComplete;
            _skeletonAnimation.UpdateLocal += _skeletonAnimation_UpdateLocal1;
            _skeletonAnimation.UpdateWorld += _skeletonAnimation_UpdateWorld;
        }

        private void _skeletonAnimation_UpdateComplete(ISkeletonRenderer skeletonRenderer)
        {
            XLogger.LogInfo("Default", $"UpdateComplete,{skeletonRenderer},在Skeleton中所有骨骼的世界值计算完成后触发该事件");
        }

        private void _skeletonAnimation_UpdateWorld(ISkeletonRenderer skeletonRenderer)
        {
            XLogger.LogInfo("Default", $"UpdateWorld,{skeletonRenderer},在计算了Skeleton中所有骨骼的世界值后触发该事件");
        }

        private void _skeletonAnimation_UpdateLocal1(ISkeletonRenderer skeletonRenderer)
        {
            XLogger.LogInfo("Default", $"UpdateLocal,{skeletonRenderer},在该帧动画更新完成并应用于skeleton的局部值之后触发该事件");
        }

        //
        private void _skeletonAnimation_BeforeApply(ISkeletonAnimation animated)
        {
            XLogger.LogInfo("Default", $"BeforeApply,{animated},在应用该帧动画之前触发该事件");
        }


        private void setAnimationState()
        {
            float timeScale = _skeletonAnimation.timeScale;
            _animationState.TimeScale = 0.5f;

            XLogger.LogInfo("Default", $"SetAnimationState,{timeScale},{_animationState.TimeScale}");

            TrackEntry entry = _skeletonAnimation.AnimationState.SetAnimation(0, _animationProperties, true);
        }

        private void animationStateCallback()
        {
            _animationState.Start += _animationState_Start;
            _animationState.Interrupt += _animationState_Interrupt;
            _animationState.Complete += _animationState_Complete;
            _animationState.End += _animationState_End;
            _animationState.Dispose += _animationState_Dispose;
            _animationState.Event += _animationState_Event;
        }

        private void _animationState_Event(TrackEntry trackEntry, Spine.Event e)
        {
            XLogger.LogInfo("Default", $"Event,{trackEntry},{e},触发了用户定义事件");
        }

        private void _animationState_Dispose(TrackEntry trackEntry)
        {
            XLogger.LogInfo("Default", $"Dispose,{trackEntry},已销毁动画及其的 TrackEntry");
        }

        private void _animationState_End(TrackEntry trackEntry)
        {
            XLogger.LogInfo("Default", $"End,{trackEntry},动画播放停止");
        }

        private void _animationState_Complete(TrackEntry trackEntry)
        {
            XLogger.LogInfo("Default", $"Complete,{trackEntry},无中断地完成了动画播放");
        }

        private void _animationState_Interrupt(TrackEntry trackEntry)
        {
            XLogger.LogInfo("Default", $"Interrupt,{trackEntry},动画播放中断");
        }

        private void _animationState_Start(TrackEntry trackEntry)
        {
            XLogger.LogInfo("Default", $"Start,{trackEntry},动画播放开始");
        }
    }
}
