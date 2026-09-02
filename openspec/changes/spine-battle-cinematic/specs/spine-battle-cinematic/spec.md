# spine-battle-cinematic 增量规格

## ADDED Requirements

### Requirement: 战斗演出可由 B 键触发

系统在演出处于待机(Ready)状态且玩家按下 B 键时,SHALL 开始一轮战斗演出:攻击方 B 播放配置的攻击动画(如 Attack1),受击方 A 保持待机动画;演出进行中重复按键 MUST 被忽略。

#### Scenario: 待机态触发演出

- **WHEN** 演出状态为 Ready 且玩家按下 B 键
- **THEN** 攻击方 B 开始播放配置的攻击动画,状态进入前摇(Windup)阶段,受击方 A 继续循环待机动画

#### Scenario: 演出进行中忽略按键

- **WHEN** 演出处于前摇/打击/后摇任一阶段且玩家再次按下 B 键
- **THEN** 演出不被打断、不重新触发,攻击动画从当前帧继续播放

### Requirement: 节拍由攻击方 clip 时间与可配置时间参数驱动

系统 SHALL 以攻击方 B 当前动画轨道的 clip 时间(AnimationTime)为主时钟,并在该时间经过配置的 `_attackStartTime`(前摇结束/打击开始)与 `_attackEndTime`(打击结束/后摇开始)时各执行一次对应节拍,同一节拍在单轮演出内 MUST 只触发一次。

#### Scenario: 到达攻击开始时间触发打击节拍

- **WHEN** B 攻击动画 clip 时间首次达到或超过 `_attackStartTime`
- **THEN** 系统执行一次打击节拍:攻击方 B 进入慢放、受击方 A 播放 Hit 并定格于 `_hitFrameTime` 帧、双人执行拉近放大

#### Scenario: 到达攻击结束时间触发释放节拍

- **WHEN** B 攻击动画 clip 时间首次达到或超过 `_attackEndTime`
- **THEN** 系统执行一次释放节拍:攻击方 B 恢复常速继续播放自身后摇段、受击方 A 解除定格续播 Hit 剩余帧、双人执行缩小回原位

### Requirement: 慢放仅作用于攻击方动画轨道

系统在打击节拍内 SHALL 将攻击方 B 当前动画轨道的 TimeScale 调整为配置的慢放倍率(默认 0.25),在释放节拍恢复为 1.0;整轮演出中 SHALL NOT 修改全局时间刻度 `Time.timeScale`。

#### Scenario: 慢放生效且全局时间恒定

- **WHEN** 打击节拍处于进行中
- **THEN** B 动画轨道的 TimeScale 等于慢放倍率,其攻击动画在真实时间中慢速推进,而 `Time.timeScale` 保持 1.0 不变

#### Scenario: 释放节拍恢复常速

- **WHEN** 释放节拍已触发
- **THEN** B 动画轨道 TimeScale 恢复 1.0,后续后摇段以常速推进

### Requirement: 受击方定格与续播

受击方 A 在打击节拍开始时 SHALL 播放 Hit 动画并冻结在 `_hitFrameTime` 对应的受击帧(轨道 TimeScale 为 0),冻结期间 MUST 保持该姿态;释放节拍开始时 SHALL 解除冻结并从该帧继续播放,播完 Hit 剩余部分后回到待机循环。

#### Scenario: 受击帧定格

- **WHEN** 打击节拍触发
- **THEN** A 播放 Hit 并恒定显示 `_hitFrameTime` 时刻的受击姿态,直至释放节拍触发

#### Scenario: 解除冻结续播后摇

- **WHEN** 释放节拍触发
- **THEN** A 从被冻结的受击帧继续播放 Hit 剩余部分,播完后自动回到待机循环 Idie

### Requirement: 无相机聚焦(移动与缩放角色实现)

整轮演出中演示相机的 Transform SHALL 保持不变;拉近放大效果 SHALL 通过攻击方与受击方角色自身的位置移动与缩放实现——打击节拍将双人从 Home 锚点补间至 Focus 锚点并按配置倍率放大,释放节拍将双人补间回 Home 锚点与原缩放。

#### Scenario: 相机保持不动

- **WHEN** 一轮演出从触发到结束的任意时刻
- **THEN** 演示相机的位置与旋转均未发生变化

#### Scenario: 拉近放大与回位

- **WHEN** 打击节拍触发
- **THEN** 双人由 Home 位置向 Focus 位置平滑移动并放大至配置倍率,过程中保持各自朝向(含镜像角色的负 X 缩放)不被破坏

- **WHEN** 释放节拍触发
- **THEN** 双人平滑移动回 Home 位置并恢复原缩放,补间结束后状态回到待机位置

### Requirement: 单轮演出结束复位并可再次触发

一轮演出结束时(B 播完攻击动画后摇、A 播完 Hit 剩余帧、双人回到 Home 位置),双方 SHALL 循环待机动画且演出状态回到 Ready,MUST 允许再次按 B 键开始新一轮演出。

#### Scenario: 演出完整结束复位

- **WHEN** B 攻击动画播放完成且 A 的 Hit 剩余帧播放完成且双人回到 Home 位置
- **THEN** A 与 B 均循环 Idie,演出状态回到 Ready,再次按下 B 键可开始新一轮演出
