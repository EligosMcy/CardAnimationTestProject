# Spine 双人战斗演出(spine-battle-cinematic)

## Why

需要验证"Spine 动画 + 战斗慢动作演出"的方案可行性:两个蜘蛛角色(Spider / Spider_Corrupted)对战时,攻击方打出蓄力前摇后,在命中瞬间做慢动作打击、受击方定格受击帧,同时双人拉近放大再回落,从而在**不移动相机**的前提下获得镜头聚焦感。当前项目只有 `DefaultSpine.cs` 这类单角色试验脚本,没有可复用的"演出编排 + 角色动画驱动"分层,无法直接承载该演出。

## What Changes

- 新增一条 **世界空间(非 UI Canvas)双人战斗演出**演示场景:角色 A(受击)、B(攻击)使用 `Assets/SpineTest/Spine/Spider` 的 `Spider` 与 `Spider_Corrupted` 骨骼资产(动画:Idie/Hit/Attack1/Attack2/Death)。
- 按下 B 键(且演出处于待机态)触发一轮完整演出:待机 → B 前摇 → 命中节拍(慢放 + 受击定格 + 拉近放大)→ 释放节拍(恢复 + 后摇 + 缩小回位)→ 双方回到 Idie。
- 引入 **3 个可调参数**(Editor 中直接调,不改代码):`_attackStartTime`(B 攻击 clip 内"前摇结束/打击起点")、`_attackEndTime`(B 攻击 clip 内"打击结束/后摇起点")、`_hitFrameTime`(A 的 Hit clip 内被定格采样的受击帧)。Attack1 拆分锚点初值由 `Spider.json` 关键帧解析得出(约 0.57s / 0.80s / Hit 前段),实施后在场景中精调。
- 实现**无相机运镜**:拉近放大通过移动/缩放角色自身 Transform(Home/Focus 两组场景锚点 + 乘性缩放),相机全程不动;慢放仅作用于攻击方动画的 `TrackEntry.TimeScale`,不动全局 `Time.timeScale`。
- 脚本拆分为可插拔的三层:`SpineActor`(封装全部 Spine API,每角色一份)、`SpineBattleDirector`(唯一决策者:输入监听 + 状态机 + 时间节拍)、`BattleStage`(唯一执行者:双人补间)。沿用项目代码规范(命名、XLogger、SO 后缀等)。

## Capabilities

### New Capabilities

- `spine-battle-cinematic`: Spine 角色双人对战慢动作演出能力——B 键触发、三时间参数驱动的节拍编排、受击帧定格/恢复、无相机移动+缩放运镜。

### Modified Capabilities

<!-- 无既有能力需求变更 -->

## Impact

- 新增代码:`Assets/SpineTest/Battle/` 下 `SpineActor.cs`、`SpineBattleDirector.cs`、`BattleStage.cs`(均不引入新 asmdef,沿用 Assembly-CSharp + `SpineTest.Battle` 命名空间,依赖 spine-unity 4.3 与 ShowX.Utils 的 XLogger)。
- 新增场景/预制体资产:双人对战演示场景(含 A/B 角色、Home/Focus 锚点、演示相机)。
- 依赖:复用现有 spine-unity 4.3 运行时与 URP 17 项目管线,不新增第三方依赖(不引入 DOTween)。
- 参考但不修改:`DefaultSpine.cs`(旧单角色试验脚本,若与演示场景冲突再单独处理)。
- 不做:UI Canvas/UISkeletonGraphic 方案、相机动画、Attack2 第二招式时间组(预留 SO 升级点)、命中特效/音效、连段队列。
