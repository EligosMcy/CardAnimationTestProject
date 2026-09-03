# spine-battle-cinematic 增量规格(相机晃动 + 参数收敛)

## ADDED Requirements

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

## REMOVED Requirements

### Requirement: 表现窗口内 FoucsAnchorGroup 冲击平移

**Reason**: 平移冲击改为相机晃动承载,观感与配置面更集中。
**Migration**: 相关平移参数与逻辑不再使用;表现窗口仅保留相机晃动,舞台对象不再平移。

### Requirement: 演出对微缩放与晃动(时机可配置)

**Reason**: 演出对自身晃动由相机晃动取代。
**Migration**: `_shakeStartWithShift` 等参数不再使用;若需"局部抖动"类表现,后续另立能力扩展。
