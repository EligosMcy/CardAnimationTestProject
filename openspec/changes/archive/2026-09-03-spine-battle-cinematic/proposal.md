# Spine 多人战斗演出(spine-battle-cinematic)

## Why

需要验证"Spine 动画 + 战斗演出"的方案可行性:场景中摆放四只蜘蛛(SpiderA~D,使用 Spider / Spider_Corrupted 骨骼资产),按 B 键触发一轮双人(受击方 + 攻击方)对决演出——在 AttackStart 节点打击瞬间,攻击方跳转命中帧定格、受击方定格受击帧(双定格),双人瞬时贴入特写(FocusAnchor 挂载 + 放大),SpiderGroup 与 background 轻微放大,幕布打开;随后进入约 1 秒的表现窗口:右侧攻击方冲击使整个 FoucsAnchorGroup 向左缓动,期间两只蜘蛛叠加微小缩放与晃动(晃动时机可配置),窗口结束即解除定格常速续播并缩小回到各自 Home 原位。全程不移动相机。当前代码/场景已实现一版"退场点(ExitAnchor)缓退"演出(规划产物刚据此修订过),现改为更直接的"冲击平移 + 抖动"表现,并移除退场点位运动。

## What Changes

- 四蜘蛛(A/B/C/D)在场景启动时统一摆位:各自世界位置对齐 `HomeAnchorA~D`,并循环播放待机动画 Idie(摆位由 Stage 负责,Actor 自播待机),不依赖场景手工摆放。
- 演出对改为**枚举可配置**:`SpineBattlePair`(预置 `Pair_AB` 与 `Pair_CD`),每值对应一组"受击方 + 攻击方"引用;默认 `Pair_AB` = SpiderA 受击 / SpiderB 攻击,切换 `Pair_CD` = SpiderC 受击 / SpiderD 攻击,节拍与舞台逻辑不感知角色名。
- B 键(Ready 态)触发一轮演出,节拍流程(新):
  - 触发点仍在 **AttackStart 节点**(攻击方 clip 时间到达 `_attackStartTime`,轮询实现;攻击资产后续加事件可平滑切换)。
  - 打击节拍:演出对两只蜘蛛**瞬时(无补间)**作为子物体挂载到各自 FocusAnchor、贴齐锚点并按 `_focusScaleMultiplier` 放大;SpiderGroup 与 `background` 同步瞬时放大至较小的 `_groupScaleMultiplier`;打开 Forward UI 幕布;攻击方跳转 `_attackFreezeFrame` 帧定格 + 受击方 Hit 定格于 `_hitFrameTime` 帧(**双定格,双方动画完全静止**)。
  - **表现窗口(时长 `_presentationDuration`,默认 1.0s,即"定格一秒扩大")**:定格期间,因攻击方在右/受击方在左,整个 `FoucsAnchorGroup`(连同演出对)以缓动向左平移 `_groupShiftDistance`、历时 `_groupShiftDuration`(可调);平移期间/平移结束后(由 bool `_shakeStartWithShift` 控制时机),两只蜘蛛在各自锚点局部叠加**微缩放与晃动**(幅度/速度可调),持续到表现窗口结束。
  - 窗口结束 = **恢复节拍**:解除双定格、双方自定格帧以常速续播(攻击方续播 Attack 后摇直至播完,受击方续播 Hit 剩余帧);`FoucsAnchorGroup` 平移回原位;SpiderGroup/`background` 复原;幕布关闭;两只蜘蛛以 `_returnDuration`(默认 0.4s)缩小回到 Home 原位(还原演出前父级与本地 TRS)。
- **移除**:原"定格缓退到退场点(ExitAnchor)"的位移效果;`ExitAnchorA/B` 与 `ExitAnchorGroup` 场景对象删除,Stage 不再引用。
- 实现仍为三层:`SpineActor`(纯 Spine 封装)/ `SpineBattleDirector`(输入 + 状态 + 节拍 + 演出对)/ `BattleStage`(摆位、挂载、组平移、晃动、回位执行),沿用项目代码规范。

## Capabilities

### New Capabilities

- `spine-battle-cinematic`: Spine 多人战斗演出能力——四角色初始化摆位待机、枚举可配置演出对(受击/攻击)、B 键触发节拍、层级挂载式瞬时聚焦 + 组/背景轻微放大、双定格(攻击跳帧 + 受击受击帧)、表现窗口内 FoucsAnchorGroup 冲击平移 + 演出对微缩放晃动(bool 控制晃动时机)、恢复后缩小回位、无相机运镜。

### Modified Capabilities

<!-- 无既有能力需求变更 -->

## Impact

- 代码:`Assets/SpineTest/Battle/` 下 `SpineActor.cs`(沿用,含 `JumpFreezeAt`)、`SpineBattleDirector.cs`(删退场/恢复触发改表现窗口定时)、`BattleStage.cs`(删 `StartRetreat`/退场协程/ExitAnchor 字段;新增 FoucsAnchorGroup 平移与演出对晃动执行);不引入新 asmdef,沿用 Assembly-CSharp + `SpineTest.Battle`,依赖 spine-unity 4.3 与 XLogger。
- 场景资产:保留 `SpiderGroup`(SpiderA~D)、`HomeAnchorA~D`、`FocusAnchorA/B`(FoucsAnchorGroup 下)、`background`、Forward UI 幕布;**删除 `ExitAnchorGroup/ExitAnchorA/B`**;仅保留一份 BattleStage 组件(Director 引用的一处)。
- 参数(Director):`_attackStartTime`(0.7)、`_attackFreezeFrame`(0.75)、`_hitFrameTime`(0.3)、`_presentationDuration`(1.0,表现窗口/定格时长)、`_returnDuration`(0.4);Stage:`_focusScaleMultiplier`(2)、`_groupScaleMultiplier`(1.1)、`_groupShiftDistance`(默认左移 0.3)、`_groupShiftDuration`、晃动幅度/速度与 `_shakeStartWithShift`(bool)。
- 依赖:复用 spine-unity 4.3 与 URP 17,不新增第三方依赖。
- 不做:同屏 2v2 双对决、UI Canvas/UISkeletonGraphic、相机动画、基于 TimeScale 的慢放、命中特效/音效、连段队列;参考但不修改 `DefaultSpine.cs`。
- 观感调参(平移距离/缓动、晃动幅度与时机、窗口时长等)由用户人工在编辑器中完成,不设自动化编辑器校验步骤。
