# 动画续播延迟到回 Home 完成后(fix-spine-resume-after-return)

## Why

当前实现中,恢复节拍(表现窗口到时)在触发"缩小回 Home"的**同时**立即对演出对 `Resume()`——`_returnDuration`(0.4s)的回位过程里攻击方/受击方动画已在继续播放,观感上"缩放回 Home 与动画续播"重叠。期望效果:**表现窗口结束后双方保持定格姿态完成回 Home(缩小回位/组复原),动画自回 Home(ReturnHome)完成、还原父级与本地 TRS 之后才开始从各自定格帧常速续播**。

## What Changes

- `SpineBattleDirector` 恢复节拍不再立即 `Resume()`:表现窗口到时只执行"幕布关闭 + `Stage.RecoverPresentation`(回位)",期间演出对动画保持定格(TimeScale=0)。
- **续播时机后移**:由 Stage `OnReturnHomeCompleted`(回位补间完成且已还原缓存父级/本地 TRS)驱动,才令受击方/攻击方 `Resume()`,自各自定格帧(`_hitFrameTime`/`_attackFreezeFrame`)常速续播。
- 收尾双条件保留:攻击方续播至 Attack 播完(动画结束事件)与回位完成均达成 → 双方 `PlayIdle()`、状态回 Ready;处理事件先后。
- `BattleStage` 无需改动(回位序列与完成回调已具备);表现参数、节拍与回位时长不变。

## Capabilities

### New Capabilities

<!-- 无新能力 -->

### Modified Capabilities

- `spine-battle-cinematic`: 修正"恢复节拍解除定格、续播并缩小回位"的需求时序——演出对动画续播 MUST 在缩小回 Home 完成(还原缓存父级与本地 TRS)之后才开始;表现窗口结束至回位完成期间双方 SHALL 保持定格姿态。

## Impact

- 代码:`Assets/SpineTest/Battle/Scripts/SpineBattleDirector.cs`(`triggerRecoverBeat` 移除 Resume;`handleReturnHomeCompleted` 中按 Recover 态触发续播;注释/阶段语义更新)。`BattleStage.cs`/`SpineActor.cs` 不改。
- 场景/资产/参数:无改动。
- 关联变更:依赖 `fix-spine-second-round-scale`(轮次基准修复)与 `spine-battle-cinematic` 的实现;实施顺序建议在其后。
- 验证:用户人工按 B 观察——表现窗口结束后的回位过程动画静止、回 Home 后才从定格帧续播并正常收尾(无自动化编辑器校验步骤)。
