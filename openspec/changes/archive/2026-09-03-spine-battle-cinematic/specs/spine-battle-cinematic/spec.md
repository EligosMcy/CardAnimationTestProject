# spine-battle-cinematic 增量规格

## ADDED Requirements

### Requirement: 四角色初始化摆位与待机

系统在场景启动时 SHALL 将四只蜘蛛(SpiderA/SpiderB/SpiderC/SpiderD)的世界位置分别对齐 HomeAnchorA/HomeAnchorB/HomeAnchorC/HomeAnchorD,并令每只蜘蛛循环播放配置的待机动画(Idie),不依赖美术在场景中的手工摆放。

#### Scenario: 启动后四角色归位并待机

- **WHEN** 演示场景加载完成进入运行
- **THEN** 四只蜘蛛各自位于对应 HomeAnchor 的世界位置,且均循环播放 Idie 动画

#### Scenario: 非演出成员保持 Home 待机

- **WHEN** 一轮演出进行中且某蜘蛛不属于当前演出对
- **THEN** 该蜘蛛保持位于其 HomeAnchor 并继续循环 Idie,不参与任何位移与缩放

### Requirement: 演出对由枚举配置且攻击/受击引用可调

系统 SHALL 通过演出对枚举(SpineBattlePair)配置每轮演出的两个角色,每个枚举值对应一组"受击方 + 攻击方"角色引用;默认枚举值 Pair_AB 映射受击方=SpiderA、攻击方=SpiderB,预置枚举值 Pair_CD 映射受击方=SpiderC、攻击方=SpiderD;切换枚举值后,新一轮演出 MUST 使用新映射的角色,且无需改动节拍与舞台逻辑。

#### Scenario: 默认演出对 AB

- **WHEN** 演出对为 Pair_AB 且按 B 键触发演出
- **THEN** 受击方为 SpiderA、攻击方为 SpiderB,整轮演出作用于该两名角色

#### Scenario: 切换演出对为 CD

- **WHEN** 演出对切换为 Pair_CD 且按 B 键触发演出
- **THEN** 受击方为 SpiderC、攻击方为 SpiderD,整轮演出作用于该两名角色,SpiderA/SpiderB 保持 Home 待机

### Requirement: 战斗演出可由 B 键触发

系统在演出处于待机(Ready)状态且玩家按下 B 键时,SHALL 开始一轮战斗演出:当前演出对的攻击方播放配置的攻击动画(如 Attack1),受击方保持待机动画;演出进行中重复按键 MUST 被忽略。

#### Scenario: 待机态触发演出

- **WHEN** 演出状态为 Ready 且玩家按下 B 键
- **THEN** 当前演出对的攻击方开始播放配置的攻击动画,状态进入前摇(Windup)阶段,受击方继续循环待机动画

#### Scenario: 演出进行中忽略按键

- **WHEN** 演出处于前摇/打击/表现窗口任一阶段且玩家再次按下 B 键
- **THEN** 演出不被打断、不重新触发,攻击动画从当前帧继续播放

### Requirement: 打击节拍由攻击方 clip 时间触发

系统 SHALL 以当前演出对攻击方动画轨道的 clip 时间(AnimationTime)为主时钟,当前摇阶段该时间首次达到配置的 `_attackStartTime`(AttackStart 节点/前摇结束)时执行一次打击节拍,同一节拍在单轮演出内 MUST 只触发一次;定格开始后攻击方动画不再推进,后续恢复节拍不再以 clip 时间为触发条件。

#### Scenario: 到达 AttackStart 触发打击节拍

- **WHEN** 攻击方攻击动画 clip 时间首次达到或超过 `_attackStartTime`
- **THEN** 系统执行一次打击节拍:攻击方跳帧定格、受击方 Hit 定格、演出对瞬时聚焦放大、打开 Forward UI 幕布,并进入表现窗口

### Requirement: 打击节拍瞬时聚焦并双定格

系统在打击节拍触发时 SHALL 在同一帧内完成:演出对两只蜘蛛作为子物体瞬时挂载到各自 FocusAnchor 并贴齐锚点位置、按 `_focusScaleMultiplier` 放大——该过程 MUST 不存在任何位置或缩放的渐变补间;攻击方将当前攻击轨道的 clip 时间跳转并锁定为 `_attackFreezeFrame`(轨道 TimeScale=0),受击方播放 Hit 并将 clip 时间锁定为 `_hitFrameTime`(轨道 TimeScale=0),双方动画 SHALL 完全静止;SpiderGroup 与 background 同步按 `_groupScaleMultiplier` 放大,该倍率 MUST 小于 `_focusScaleMultiplier`;同时打开 Forward UI 幕布。演出对挂载后保持位于 FocusAnchor 下,不做任何退场点位的位移。

#### Scenario: 瞬时挂载与放大

- **WHEN** 打击节拍触发
- **THEN** 受击方与攻击方在同一帧内成为各自 FocusAnchor 的子物体并贴齐锚点位置,localScale 乘以 `_focusScaleMultiplier`,全程不存在位置/缩放的渐变过程

#### Scenario: 攻击方跳帧定格

- **WHEN** 打击节拍触发
- **THEN** 攻击方动画轨道跳转并定格在 `_attackFreezeFrame` 帧(TimeScale=0),定格期间姿态恒定不变,直至恢复节拍

#### Scenario: 受击方受击帧定格

- **WHEN** 打击节拍触发
- **THEN** 受击方播放 Hit 并恒定显示 `_hitFrameTime` 时刻的受击姿态(TimeScale=0),直至恢复节拍

#### Scenario: 组与背景轻微放大

- **WHEN** 打击节拍触发
- **THEN** SpiderGroup 与 background 的缩放乘以 `_groupScaleMultiplier`(小于 `_focusScaleMultiplier`),组内非演出成员随组轻微放大,演出成员不因组缩放被二次放大

#### Scenario: 幕布打开

- **WHEN** 打击节拍触发
- **THEN** Forward UI 幕布被激活显示,直至恢复节拍触发

### Requirement: 表现窗口内 FoucsAnchorGroup 冲击平移

打击节拍后系统 SHALL 进入固定时长的表现窗口(时长由 `_presentationDuration` 决定,默认 1.0 秒,期间演出对保持双定格与放大尺寸);表现窗口内,因攻击方位于右侧冲击左侧受击方,承载 FocusAnchor 的 FoucsAnchorGroup SHALL 以缓动曲线向左平移一段可配置距离(`_groupShiftDistance`),平移历时 `_groupShiftDuration`;恢复节拍触发时 FoucsAnchorGroup SHALL 平移回原位。系统 SHALL NOT 将演出对移向任何退场点(ExitAnchor 已废弃)。

#### Scenario: 定格保持并放大

- **WHEN** 打击节拍触发后、表现窗口结束前
- **THEN** 演出对两只蜘蛛保持各自的定格帧姿态与放大尺寸,动画完全静止

#### Scenario: 组向左缓动平移

- **WHEN** 表现窗口进行中
- **THEN** FoucsAnchorGroup(连同其下演出对)以缓动曲线向左平移 `_groupShiftDistance`,时长 `_groupShiftDuration`,平移量与缓动形态由场景参数决定

#### Scenario: 组回原位

- **WHEN** 恢复节拍触发
- **THEN** FoucsAnchorGroup 平移回到其演出前位置,与组/背景复原、缩小回位同步进行

### Requirement: 演出对微缩放与晃动(时机可配置)

表现窗口内,系统 SHALL 令演出对两只蜘蛛在各自 FocusAnchor 局部叠加微小的缩放脉冲与位置抖动(幅度与速度由可调参数决定),作为打击冲击的抖动表现;晃动是否与组平移同步启动由 bool 参数 `_shakeStartWithShift` 决定——为 true 时 SHALL 于 FoucsAnchorGroup 开始平移时即启动晃动,为 false 时 SHALL 于组平移完成后再启动;晃动持续至表现窗口结束(恢复节拍)。

#### Scenario: 随动晃动

- **WHEN** `_shakeStartWithShift` 为 true 且表现窗口开始
- **THEN** 两只蜘蛛随组平移一开始即开始微小缩放与晃动,直至恢复节拍

#### Scenario: 滞后晃动

- **WHEN** `_shakeStartWithShift` 为 false 且 FoucsAnchorGroup 平移完成
- **THEN** 两只蜘蛛开始微小缩放与晃动,直至恢复节拍;组平移期间不晃动

### Requirement: 恢复节拍解除定格、续播并缩小回位

系统在表现窗口结束(恢复节拍)时 SHALL:攻击方动画轨道 TimeScale 恢复 1.0、自 `_attackFreezeFrame` 帧以常速续播攻击动画后摇直至播完;受击方解除定格、自 `_hitFrameTime` 帧以常速续播 Hit 剩余帧;关闭 Forward UI 幕布;FoucsAnchorGroup 平移回原位;SpiderGroup 与 background 同步恢复基准缩放;演出对两只蜘蛛以 `_returnDuration`(默认 0.4 秒)从当前位置缩小回到 Home 原位,结束后 SHALL 回到其演出前的父级与本地位置/缩放(Home 态)。

#### Scenario: 解除定格常速续播

- **WHEN** 恢复节拍触发
- **THEN** 攻击方与受击方均解除定格,各自从定格帧以常速(TimeScale=1.0)继续播放剩余动画

#### Scenario: 缩小回位

- **WHEN** 恢复节拍触发
- **THEN** 两只蜘蛛以 `_returnDuration` 时长移动到各自 Home 原位,期间缩放从放大倍率回落至基准,结束后还原演出前父级与本地变换

#### Scenario: 组平移复原与组/背景复原

- **WHEN** 恢复节拍触发
- **THEN** FoucsAnchorGroup 平移回原位,SpiderGroup 与 background 的缩放恢复基准(乘以 1)

#### Scenario: 幕布关闭

- **WHEN** 恢复节拍触发
- **THEN** Forward UI 幕布被关闭隐藏

### Requirement: 相机保持不动

整轮演出中演示相机的 Transform SHALL 保持不变;拉近/冲击效果 SHALL 通过"演出对挂载 FocusAnchor + 乘性放大"、"SpiderGroup/background 轻微放大"与"FoucsAnchorGroup 平移"实现,不得通过移动相机、改变画幅或 FOV,且整轮演出中 SHALL NOT 修改全局时间刻度 `Time.timeScale`。

#### Scenario: 演出全程相机不动

- **WHEN** 一轮演出从触发到结束的任意时刻
- **THEN** 演示相机的位置与旋转均未发生变化

#### Scenario: 镜像朝向不被破坏

- **WHEN** 演出角色经历挂载、放大、定格、晃动、缩小回位的全过程
- **THEN** 各角色缩放均为对基准缩放的乘性变化,含镜像角色的负 X 缩放在内的朝向保持不变,结束时恢复演出前父级与本地缩放

### Requirement: 单轮演出结束复位并可再次触发

一轮演出结束时(攻击方自定格帧常速续播并播完攻击动画后摇、受击方续播完 Hit 剩余帧、FoucsAnchorGroup 已回原位、双人缩小回位完成且还原演出前父级与本地变换、SpiderGroup/background 复原),系统 SHALL 令受击方与攻击方循环 Idie 并将演出状态回到 Ready,MUST 允许再次按 B 键开始新一轮演出;若攻击方动画结束与回位补间完成二者先后到达,系统 MUST 等待两个条件均达成后再复位。

#### Scenario: 演出完整结束复位

- **WHEN** 攻击方攻击动画播放完成 且 双人缩小回位完成
- **THEN** 受击方与攻击方均循环 Idie,演出状态回到 Ready,再次按下 B 键可开始新一轮演出

#### Scenario: 攻击动画先于回位结束

- **WHEN** 攻击方攻击动画播放完成但双人缩小回位补间仍在进行
- **THEN** 系统等待回位补间完成并还原缓存变换后才回到 Ready 并播放 Idie
