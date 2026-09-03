# spine-battle-cinematic Specification

## Purpose
TBD ... Update Purpose after archive

## Requirements

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

### Requirement: 恢复节拍解除定格、续播并缩小回位

系统在表现窗口结束(恢复节拍)时 SHALL:攻击方动画轨道 TimeScale 恢复 1.0、自 `_attackFreezeFrame` 帧以常速续播攻击动画后摇直至播完;受击方解除定格、自 `_hitFrameTime` 帧以常速续播 Hit 剩余帧;关闭 Forward UI 幕布;SpiderGroup 与 background 同步恢复基准缩放;演出对两只蜘蛛以 `_returnDuration`(默认 0.15 秒,由配置资产统一提供)从当前位置缩小回到 Home 原位,结束后 SHALL 回到其演出前的父级与本地位置/缩放(Home 态)。

#### Scenario: 解除定格常速续播

- **WHEN** 恢复节拍触发
- **THEN** 攻击方与受击方均解除定格,各自从定格帧以常速(TimeScale=1.0)继续播放剩余动画

#### Scenario: 缩小回位

- **WHEN** 恢复节拍触发
- **THEN** 两只蜘蛛以 `_returnDuration` 时长移动到各自 Home 原位,期间缩放从放大倍率回落至基准,结束后还原演出前父级与本地变换

#### Scenario: 组/背景复原

- **WHEN** 恢复节拍触发
- **THEN** SpiderGroup 与 background 的缩放恢复基准(乘以 1)

#### Scenario: 幕布关闭

- **WHEN** 恢复节拍触发
- **THEN** Forward UI 幕布被关闭隐藏

### Requirement: 相机保持不动

整轮演出中系统 SHALL NOT 修改全局时间刻度 `Time.timeScale`;除表现窗口内的相机晃动外,演示相机 Transform SHALL 保持不变。拉近效果 SHALL 通过"演出对挂载 FocusAnchor + 乘性放大"与"SpiderGroup/background 轻微放大"实现,不得改变画幅或 FOV;冲击效果由表现窗口内的相机晃动承载,晃动以演出前相机位姿为基准小幅抖动,恢复节拍触发后 SHALL 停止并精确还原相机基准。

#### Scenario: 相机除表现窗口晃动外保持不动

- **WHEN** 一轮演出中除表现窗口外的任意时刻
- **THEN** 演示相机的位置与旋转均未发生变化

#### Scenario: 表现窗口晃动并还原相机基准

- **WHEN** 表现窗口进行中
- **THEN** 演示相机以演出前基准为中心小幅晃动(幅度/速度可调);恢复节拍触发后停止晃动并精确还原演出前相机位姿

#### Scenario: 镜像朝向不被破坏

- **WHEN** 演出角色经历挂载、放大、定格、晃动、缩小回位的全过程
- **THEN** 各角色缩放均为对基准缩放的乘性变化,含镜像角色的负 X 缩放在内的朝向保持不变,结束时恢复演出前父级与本地缩放

### Requirement: 单轮演出结束复位并可再次触发

一轮演出结束时(攻击方自定格帧常速续播并播完攻击动画后摇、受击方续播完 Hit 剩余帧、双人缩小回位完成且还原演出前父级与本地变换、SpiderGroup/background 复原),系统 SHALL 令受击方与攻击方循环 Idie 并将演出状态回到 Ready,MUST 允许再次按 B 键开始新一轮演出;若攻击方动画结束与回位补间完成二者先后到达,系统 MUST 等待两个条件均达成后再复位。

#### Scenario: 演出完整结束复位

- **WHEN** 攻击方攻击动画播放完成 且 双人缩小回位完成
- **THEN** 受击方与攻击方均循环 Idie,演出状态回到 Ready,再次按下 B 键可开始新一轮演出

#### Scenario: 攻击动画先于回位结束

- **WHEN** 攻击方攻击动画播放完成但双人缩小回位补间仍在进行
- **THEN** 系统等待回位补间完成并还原缓存变换后才回到 Ready 并播放 Idie

### Requirement: 演出对多次轮播时挂载放大与回 Home 还原基准一致

系统在演出对的每一轮演出开始时 SHALL 以该轮角色当前的 Home 基准(父级 + 本地位置/旋转/缩放)执行瞬时挂载与 `_focusScaleMultiplier` 放大,并在该轮结束缩小回 Home 原位时 SHALL 精确还原为该轮演出前的父级与本地 TRS;任一后续轮次 MUST NOT 继承上一轮演出的放大倍率、晃动扰动或平移中间值,首轮与后续轮次的放大尺寸与回位效果 MUST 完全一致。

#### Scenario: 连续第二轮演出放大尺寸与首轮一致

- **WHEN** 第一轮演出完整结束回到 Ready 后,再次按 B 键开始第二轮演出并到达打击节拍
- **THEN** 演出对的挂载放大尺寸与第一轮完全一致(本地缩放恰为 Home 基准 × `_focusScaleMultiplier`,未发生倍率叠加或漂移)

#### Scenario: 每轮结束均精确回 Home 原位

- **WHEN** 每一轮演出(含第二轮及以后)的恢复收尾完成
- **THEN** 演出对双方回到其各自该轮演出前的父级、本地位置、本地旋转与本地缩放,与 Home 摆位一致,不存在上轮放大/晃动值的残留

#### Scenario: 快照不受上轮中间值污染

- **WHEN** 新一轮演出开始且上一轮演出对引用/挂载缩放等轮次数据仍被保留
- **THEN** 新一轮的 Home 基准快照仍取自角色当前实际变换(干净的 Home 态),不以任何上轮缓存值改写角色后再采样

### Requirement: 待机态复位不得写回演出变换

系统 SHALL 确保仅在一轮演出进行中的舞台动作(停止晃动/组归位等清理)才允许写回演出角色的本地变换;演出处于 Ready/待机(未挂载)时,任何基于历史轮次的清理 MUST NOT 修改角色的父级、位置或缩放。

#### Scenario: 待机态触发新一轮演出前不产生变换写回

- **WHEN** 系统处于 Ready 态并即将开始新一轮演出(上一轮演出对的引用尚未被清除)
- **THEN** 在新快照采样与挂载之前,系统不因旧轮清理逻辑改变角色的本地位置/缩放(不得将上一轮放大值写回 Home 态角色)

### Requirement: 演出对动画自回 Home 完成后才开始续播

系统在表现窗口结束时(恢复节拍)SHALL 执行缩小回位(幕布关闭、SpiderGroup/background 复原、演出对缩小回 Home 原位并还原缓存父级与本地 TRS),期间演出对动画 SHALL 保持定格(动画轨道 TimeScale=0,不推进);当缩小回 Home 完成(还原完成)后,系统 SHALL 才令演出对双方从各自定格帧以常速(TimeScale=1.0)续播剩余动画;收尾仍以"攻击方续播至攻击动画播完"与"回位完成"双条件复位 Ready 并循环 Idie。

#### Scenario: 回位期间动画保持定格

- **WHEN** 表现窗口结束触发恢复节拍、演出对正在缩小回 Home(回位补间进行中)
- **THEN** 演出对双方动画不推进,保持各自的定格帧姿态(攻击方 `_attackFreezeFrame`、受击方 `_hitFrameTime`),直至回位完成

#### Scenario: 回 Home 完成后才开始续播

- **WHEN** 缩小回 Home 补间完成、演出对已还原缓存父级与本地 TRS(Stage 上抛回位完成回调)
- **THEN** 演出对双方自各自定格帧以常速续播剩余动画(攻击方续播 Attack 后摇、受击方续播 Hit 剩余帧)

#### Scenario: 回位完成后按双条件收尾

- **WHEN** 回位完成后演出对双方已续播,且攻击方攻击动画最终播放完成
- **THEN** 系统按既有收尾双条件(攻击方动画结束 + 回位完成)令双方循环 Idie、状态回 Ready,可再次触发新一轮演出

### Requirement: 恢复节拍不再以续播为先导

系统在表现窗口结束时(恢复节拍)SHALL NOT 先行解除演出对动画定格;解除定格(续播)仅允许在缩小回 Home 完成后发生。恢复节拍触发时仅执行:幕布关闭、组/背景复原与演出对缩小回 Home 的舞台动作。

#### Scenario: 恢复节拍触发时不立即续播

- **WHEN** 恢复节拍触发(表现窗口到时)
- **THEN** 演出对动画仍保持定格(TimeScale 仍为 0),系统仅执行回位相关舞台动作与幕布关闭

### Requirement: 表现窗口以相机晃动承载冲击

系统在打击节拍后进入表现窗口期间,SHALL 对演示相机施加以"演出前相机基准"为中心的小幅晃动(位移/旋转噪声),作为冲击表现,替代原"FoucsAnchorGroup 平移冲击"与"演出对微缩放晃动";恢复节拍(回位序列)触发时 SHALL 停止晃动并把演示相机精确还原到演出前基准。演出对两只蜘蛛 SHALL NOT 再做平移冲击或自身微晃动。

#### Scenario: 表现窗口内相机晃动

- **WHEN** 打击节拍触发、表现窗口进行中
- **THEN** 演示相机在演出前基准附近做小幅晃动(幅度/速度由参数决定),演出对保持挂载聚焦与定格姿态,不做整体平移或自身微晃动

#### Scenario: 恢复节拍停止晃动并还原相机

- **WHEN** 恢复节拍触发(表现窗口结束,进入定格收缩回位)
- **THEN** 相机晃动停止,演示相机位置与旋转精确还原至演出前基准;后续回位、续播与收尾期间相机保持该基准不动

### Requirement: 演出参数由单一 ScriptableObject 配置

系统 SHALL 通过唯一配置资产 `SpineBattleSettings`(ScriptableObject)提供演出全部数值/布尔/名称类可调参数(节拍时间、表现窗口与回位时长、缩放倍率、相机晃动参数、攻击 clip 名、调试开关等);SpineBattleDirector 与 BattleStage SHALL 运行期读取该资产,自身组件内 MUST NOT 再持有重复的数值参数;两组件若未配置资产 SHALL 在启动时报错并中断。默认资产数值 SHALL 与创建时当前场景的现场值一致(节拍 0.7/0.75/0.3、表现窗口 0.6、回位 0.15、焦点倍率 2、组倍率 1.1 等,以场景实读为准)。

#### Scenario: 组件从单一资产读取参数

- **WHEN** SpineBattleDirector 与 BattleStage 均已接线同一份 `SpineBattleSettings` 资产并进入运行
- **THEN** 两者的节拍/窗口/回位/倍率/相机晃动等参数均取自该资产,修改该资产文件即可整体调整演出效果,无需修改组件 Inspector 数值

#### Scenario: 未配置参数资产时中断

- **WHEN** SpineBattleDirector 或 BattleStage 的 `_settings` 为空且进入运行
- **THEN** 组件输出错误日志并中断演出初始化,不按默认值静默运行

