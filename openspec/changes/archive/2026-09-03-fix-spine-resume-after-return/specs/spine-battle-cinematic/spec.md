# spine-battle-cinematic 修复增量规格(动画续播延迟到回 Home 完成后)

## ADDED Requirements

### Requirement: 演出对动画自回 Home 完成后才开始续播

系统在表现窗口结束时(恢复节拍)SHALL 执行缩小回位(幕布关闭、SpiderGroup/background 复原、FoucsAnchorGroup 回原位、演出对缩小回 Home 原位并还原缓存父级与本地 TRS),期间演出对动画 SHALL 保持定格(动画轨道 TimeScale=0,不推进);当缩小回 Home 完成(还原完成)后,系统 SHALL 才令演出对双方从各自定格帧以常速(TimeScale=1.0)续播剩余动画;收尾仍以"攻击方续播至攻击动画播完"与"回位完成"双条件复位 Ready 并循环 Idie。

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

系统在表现窗口结束时(恢复节拍)SHALL NOT 先行解除演出对动画定格;解除定格(续播)仅允许在缩小回 Home 完成后发生。恢复节拍触发时仅执行:幕布关闭、组/背景复原、组回原位与演出对缩小回 Home 的舞台动作。

#### Scenario: 恢复节拍触发时不立即续播

- **WHEN** 恢复节拍触发(表现窗口到时)
- **THEN** 演出对动画仍保持定格(TimeScale 仍为 0),系统仅执行回位相关舞台动作与幕布关闭
