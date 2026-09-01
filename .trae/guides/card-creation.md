# 卡牌创建规范

> 本规范定义了创建新卡牌时的完整流程、存储路径、命名规则和确认机制。
> 创建任何新卡牌前，Agent 必须完整阅读并遵循本规范。
>
> **配套速查参考**：`card-creation-reference.md`（同目录下）包含所有枚举值、已有 Effect/TargetMode/GameAction/PerkCondition 清单、关键 GUID 和路径、以及 .asset YAML 模板。创建卡牌时务必同时加载该文件。

---

## 一、资产存储路径与命名规则

### 1.1 CardData（卡牌数据）

| 项目 | 规则 |
|------|------|
| 根路径 | `Assets/Resources/Data/FirstMonthResources/Cards/` |
| 分类子文件夹 | `Attack/` `Curse/` `Derived/` `Equipment/` `Power/` `Skill/` |
| 命名格式 | `{ID}_Card_{拼音}_{中文名}.asset` |
| 示例 | `10001_Card_ZhanJi_斩击.asset` |

- `ID` 为卡牌全局唯一标识，对应 `Assets/CardGameAssets/表/Data/#Card.xlsx` 表格中的 ID 列（如 `10001`）
- 拼音首字母大写，不含声调，多字拼音直接拼接（如 `DieJiaTongJi`）
- 中文名保持原样，含特殊符号如 `·`（如 `10007_Card_FuHuaHaoYin_腐化·豪饮`）
- 卡牌类型必须与子文件夹对应：Attack → `Attack/`，Equipment → `Equipment/`，以此类推

### 1.2 EquipmentData（装备数据）

| 项目 | 规则 |
|------|------|
| 路径 | `Assets/Resources/Data/FirstMonthResources/Equipments/` |
| 命名格式 | `Equip_{拼音}_{中文名}.asset` |
| 示例 | `Equip_JianQieTaiDao_见切太刀.asset` |

- 仅 `CardType = Equipment` 的卡牌需要创建
- 拼音必须与对应 CardData 的拼音部分一致
- 创建后必须在 CardData 的 `EquipmentData` 字段中设置引用

### 1.3 PerkData（特权数据）

| 项目 | 规则 |
|------|------|
| 根路径 | `Assets/Resources/Data/FirstMonthResources/Perks/` |
| 装备特权子文件夹 | `EquipmentPerk/` |
| 能力特权子文件夹 | `PowerPerk/` |
| 命名格式 | `Perk_{卡牌中文名}_{效果中文名}.asset` |
| 示例 | `Perk_麦德斯之锤_金币.asset`、`Perk_锁链之剑_群体攻击.asset` |

- 装备牌的 Perk 放入 `EquipmentPerk/`
- 能力牌的 Perk 放入 `PowerPerk/`
- 卡牌中文名直接使用中文（非拼音）
- 效果中文名用简短中文描述效果类型

### 1.4 卡面图片

| 项目 | 规则 |
|------|------|
| 根路径 | `Assets/Sprites/CardFace/` |
| 分类子文件夹 | `Attack/` `Curse/` `Derived/` `Equipment/` `Power/` `Skill/` |
| 查找方式 | 根据卡牌类型进入对应子文件夹，按卡牌中文名查找匹配的图片文件 |
| 未找到时 | 向用户询问具体使用哪张图片，列出该类型文件夹下的可用图片供选择 |

---

## 二、CardData 字段确认清单

创建卡牌前，以下所有字段必须逐一确认：

### 2.1 基础信息

| 字段 | 类型 | 说明 |
|------|------|------|
| `ID` | int | 全局唯一标识，对应 `Assets/CardGameAssets/表/Data/#Card.xlsx` 表格中的 ID 列 |
| `HeroTypeRestriction` | HeroType 枚举 | 角色类型限制，`None`=通用（默认）；诅咒/衍生/事件特殊牌应显式设为 `None` |
| `CardType` | CardType 枚举 | Attack / Skill / Power / Equipment（武器牌）/ Curse / Derived（状态牌，不可打出） |
| `Title` | string | 卡牌中文名 |
| `Description` | string | 卡牌描述，动态数值使用占位符：`{ATK}`=伤害、`{DEF}`=护甲、`{HIT}`=段数、`{MUL}`=力量倍率、`{SOUL}`=噬魂匕首击杀计数、`{SELF}`=自伤 |
| `Rarity` | CardRarity 枚举 | Starter / Common / Rare / Epic / Corrupted / Special / Unique / DynamicUnique |
| `IsTemp` | bool | 是否临时牌（由效果生成的临时卡牌） |
| `Energy` | int | 基础费用 |

### 2.2 视觉配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `Image` | Sprite | 卡面图片，从 `Sprites/CardFace/{Type}/` 按名查找 |

### 2.3 效果配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `ManualTargetBaseEffects` | List\<BaseEffect\> (SerializeReference) | 手动选择目标的效果列表，**可叠加多个**（所有效果共享同一个手动选定的敌人目标） |
| `OtherEffects` | List\<AutoTargetEffect\> | 自动选择目标的效果列表 |

- `AutoTargetEffect` 包含 `TargetMode`（目标模式）和 `BaseEffect`（效果）两部分
- 如果现有 Effect 无法实现需求，需要创建新的 Effect 类（见第五节）
- 伤害/护甲数值需同时在 `MagicNumbers` 中配置（见 2.4），执行层真值源仍是 Effect 字段，两者必须保持一致

### 2.4 魔法数字配置（MagicNumbers）

| 字段 | 类型 | 说明 |
|------|------|------|
| `MagicNumbers` | List\<CardMagicNumberData\> | 基础轨道魔法数字条目 |
| `UpgradedMagicNumbers` | List\<CardMagicNumberData\> | 升级轨道魔法数字条目 |

每个 `CardMagicNumberData` 条目包含：

| 字段 | 说明 |
|------|------|
| `MagicType` | 类型：`BasicAttack`（基础攻击）/ `BasicArmor`（基础护甲）/ `BasicSelfDamage`（基础自伤） |
| `BaseValue` | 基础值（当前轨道的伤害/护甲数值） |
| `HitCountBase` | 段数基准（仅 `BasicAttack` 有意义，默认 1；多段攻击卡在此配置基准段数，动态公式可覆写） |
| `Formula` | 公式配置（可空，空=固定值）。可选：`WeaponMultiHit`（武器多段）/ `WeaponBonus`（武器加成）/ `ExhaustWeaponCount`（消耗堆武器段数）/ `LostHealthRatio`（已损失生命比率）/ `GoldRatio`（金币比率）/ `WeaponsEquippedCount`（武器装备计数）/ `StackDamage`（叠加伤害）/ `SoulDevourerDagger`（噬魂匕首卸下） |
| `StrengthMultiplier` | 力量倍率（仅 `BasicAttack` 有意义，默认 None=×1） |

- 描述中的 `{ATK}`/`{DEF}`/`{HIT}`/`{MUL}`/`{SOUL}`/`{SELF}` 占位符由 `MagicNumbers` 运行时展开替换
- **双源一致规则**：执行层真值源是 Effect 的序列化字段（如 `DealDamageEffect._damageAmount`），显示层读取 `MagicNumbers`；两者必须由策划手动保持一致
- 力量倍率卡（如重击）使用 `DealDamageWithMultiplierEffect`（倍率在 Effect 内配置），普通伤害卡使用 `DealDamageEffect`

### 2.5 特权配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `CardPerks` | List\<CardPerkData\> | 卡牌内联特权列表 |

- `CardPerkData` 是内联可序列化类，**不是独立资产文件**
- 每个 `CardPerkData` 包含：
  - `PerkData`：引用独立的 PerkData 资产
  - `PerkScope`：作用范围（`CardTurn` / `CurrentTurn` / `Global`）

### 2.6 特性配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `Traits` | CardTrait[] | 卡牌特性标记 |
| `PlayConditions` | CardPlayCondition[] | 打出条件限制 |
| `Keywords` | CardKeyword[] | 卡牌关键词（如 Discover 发现），在侧边栏显示说明 |
| `CardTargetType` | CardTargetType | **必须显式设置**，留空会触发警告 |

CardTrait 可选值：`Exhaust`（消耗）、`Ethereal`（虚无）、`Innate`（固有）、`Retain`（保留）、`ReturnToHand`（返回手牌）、`Replay`（重放）

CardPlayCondition 可选值：`None`、`SoloPlayable`（孤牌可打）、`HasEquipmentInExhaust`、`HasAttackInDrawPile`、`NeverPlayable`、`HasAttackInDiscardPile`、`HasAttackInDrawOrDiscardPile`、`MinHandSize2`、`MinHandSize3`、`MinHandSize4`

CardTargetType 可选值：`None`（无目标）、`Self`（自身）、`Enemy`（单敌）、`AllEnemy`（全体敌人）、`SelfAndEnemy`（自身+敌人）

### 2.7 升级后属性（无 Fallback 规则）

> **关键规则**：`CanUpgrade = true` 时，所有 `Upgraded*` 字段必须完整填写。
> 代码中不做 fallback，升级后直接使用升级字段，留空会导致字段为默认值。

| 字段 | 说明 |
|------|------|
| `CanUpgrade` | 是否允许升级 |
| `UpgradedTitle` | 升级后标题（基础标题+，如"斩击+"） |
| `UpgradedEnergy` | 升级后费用 |
| `UpgradedDescription` | 升级后描述 |
| `UpgradedManualTargetBaseEffects` | 升级后手动目标效果列表 |
| `UpgradedOtherEffects` | 升级后自动目标效果列表 |
| `UpgradedMagicNumbers` | 升级后魔法数字条目（见 2.4） |
| `UpgradedCardPerks` | 升级后特权列表 |
| `UpgradedTraits` | 升级后特性 |
| `UpgradedPlayConditions` | 升级后打出条件 |
| `UpgradedKeywords` | 升级后关键词 |
| `UpgradedCardTargetType` | 升级后目标类型（必须显式设置） |

### 2.8 腐化觉醒配置（仅 Corrupted 品质）

| 字段 | 说明 |
|------|------|
| `HasAwaken` | 是否有觉醒效果（仅 `CardRarity.Corrupted` 有效） |
| `AwakenInsightThreshold` | 觉醒所需的灵视阈值（默认 80） |

### 2.9 装备配置（仅 Equipment 类型）

| 字段 | 说明 |
|------|------|
| `EquipmentData` | 引用对应的 EquipmentData 资产（创建后必须设置） |

### 2.10 特殊效果标记（运行时标记，非数据字段）

> **注意**：`CardSpecialEffect` 已移出 CardData，**不再在卡牌资产中配置**。
> 该标记现在是 `CardInstance` 的运行时属性，由战斗系统在运行时赋值：
> - `Resistance`（抗拒）→ `ResistanceContractSystem` 调用 `SetSpecialEffect(CardSpecialEffect.Resistance)`
> - `Submit`（服从）→ `SubmitWillSystem` 调用 `ApplySubmitEffect()`
>
> 费用修正由全局条件修饰器实时判定：`SubmitCostReductionModifier`（服从费用-1）、`NonSubmitCostPenaltyModifier`（支配40惩罚，服从牌豁免）。
> 创建卡牌时**无需填写任何特效字段**。

---

## 三、EquipmentData 字段确认清单

仅 `CardType = Equipment` 时需要创建。

### 3.1 基础信息

显示信息（名称/图片/描述）来自 CardData，EquipmentData 不再存储显示字段。

### 3.2 效果配置（基础 + 升级）

| 字段 | 说明 |
|------|------|
| `EquipEffects` / `UpgradedEquipEffects` | 装备时触发 |
| `UnEquipEffects` / `UpgradedUnEquipEffects` | 卸下时触发 |
| `TurnStartEffects` / `UpgradedTurnStartEffects` | 回合开始时触发 |
| `TurnEndEffects` / `UpgradedTurnEndEffects` | 回合结束时触发 |
| `EquipmentPerks` / `UpgradedEquipmentPerks` | 装备持续 Perk 引用列表 |

### 3.3 伤害与渠道

| 字段 | 说明 |
|------|------|
| `AttackDamageTiming` | **必设**。伤害触发时机：`None` / `OnCardPlay`（出牌时）/ `OnEquip`（装备时）/ `OnUnequip`（卸下时） |
| `Obstruction` | 渠道限制（Flags）：`None` / `NoBattle` / `NoShop` / `NoBattleReward` / `NoEvent` |

### 3.4 卸下去向

| 字段 | 说明 |
|------|------|
| `UnequipDestination` | 卸下后装备牌自身的去向（内部替换/缩减槽位时使用），默认 `Exhaust`（消耗堆） |
| `UpgradedUnequipDestination` | 升级后卸下时装备牌自身的去向，默认 `Exhaust` |

---

## 四、PerkData 字段确认清单

### 4.1 基础信息

| 字段 | 类型 | 说明 |
|------|------|------|
| `PerkStr` | string | 特权显示名称 |
| `Image` | Sprite | 特权图标 |

### 4.2 触发条件

| 字段 | 类型 | 说明 |
|------|------|------|
| `PerkCondition` | PerkCondition (SerializeReference) | 触发条件，决定特权何时激活 |

- 如果现有 PerkCondition 无法满足需求，需要创建新的条件类（见第五节）

### 4.3 目标选择

| 字段 | 说明 |
|------|------|
| `UseAutoTarget` | 是否使用自动目标模式 |
| `TargetMode` | 自动目标模式（SerializeReference） |
| `UseManualTarget` | 是否使用手动目标模式 |
| `UseActionCasterAsTarget` | 以触发动作的发起者为目标（反击类） |
| `UseCurrentTargetAsTarget` | 以触发动作的当前目标为目标（追加效果类） |

### 4.4 效果

| 字段 | 说明 |
|------|------|
| `ListBaseEffect` | 特权执行的额外效果列表 `List<BaseEffect>`（SerializeReference，触发时按顺序依次执行，可单独使用） |
| `IsModifier` | 是否为修改器模式（true=修改已有动作，false=创建新动作）。修改器模式强制使用 Pre 时机 |

---

## 五、新建代码文件的规则

当现有 Effect / GameAction / PerkCondition / TargetMode 无法满足需求时，需要创建新的代码文件。

### 5.1 新建 Effect

| 项目 | 规则 |
|------|------|
| 文件路径 | `Assets/Scripts/Effects/{Name}Effect.cs` |
| 命名空间 | `Effects` |
| 基类 | `BaseEffect`（位于 `Models` 命名空间） |
| 必须实现 | `GetGameAction(EffectContext context)` |
| 可选重写 | `ModifyAction(GameAction action)` — 修改器模式 |
| 可选重写 | `GetBaseDamage(CardInstance cardInstance)` — 返回预览伤害值（0=无伤害，-1=无法确定） |
| 命名规则 | `{英文描述}Effect`，如 `DealDamageEffect`、`DrawCardEffect` |

- 伤害类效果约定：普通伤害用 `DealDamageEffect`（力量倍率固定 None，可选 `_damageSource` 伤害来源，默认 Attack）；带力量倍率的攻击卡用 `DealDamageWithMultiplierEffect`（`_strengthMultiplier`）
- **双源一致**：Effect 字段是执行层真值源，创建新卡时须与卡牌 `MagicNumbers` 配置保持一致（显示层读取 MagicNumbers）

### 5.2 新建 GameAction

| 项目 | 规则 |
|------|------|
| 文件路径 | `Assets/Scripts/GameActions/{Category}/{Name}Ga.cs` |
| 命名空间 | `GameActions`（所有 Ga 统一平铺在此命名空间，子文件夹仅为文件组织） |
| 命名规则 | `{英文描述}Ga`，如 `DealDamageGa`、`DrawCardsGA` |

Category 子文件夹选择：

| 子文件夹 | 用途 |
|---------|------|
| `Base/` | 基础动作（伤害、治疗、抽牌等） |
| `Card/` | 卡牌相关（打出、弃牌、消耗等） |
| `Effect/` | 效果相关（状态添加/移除等） |
| `Enemy/` | 敌人相关（攻击、死亡、复活等） |
| `Energy/` | 能量相关 |
| `Equipment/` | 装备相关（装备、卸下等） |
| `Combat/` | 战斗相关（受击、格挡等） |
| `Hero/` | 英雄相关 |
| `Artifact/` | 遗物相关 |
| `logic/` | 战斗流程逻辑 |

### 5.3 新建 PerkCondition

| 项目 | 规则 |
|------|------|
| 文件路径 | `Assets/Scripts/PerkConditions/{Name}Condition.cs` |
| 命名空间 | `PerkConditions` |
| 基类 | `PerkCondition`（位于 `Models` 命名空间） |
| 必须实现 | `SubscribeCondition()`、`UnSubscribeCondition()`、`SatisfiesConditionIsMet()` |
| 可选重写 | `GetDefaultTargetMode()` |
| 命名规则 | `On{触发条件}Condition`，如 `OnAttackPlayedCondition`、`OnTurnStartCondition` |

### 5.4 新建 TargetMode

| 项目 | 规则 |
|------|------|
| 文件路径 | `Assets/Scripts/TargetModels/{Name}TM.cs` |
| 命名空间 | `TargetModels` |
| 基类 | `TargetMode`（位于 `Models` 命名空间） |
| 命名规则 | `{英文描述}TM`，如 `HeroTM`、`AllEnemiesTM`、`ManualTM` |

### 5.5 代码文件创建后检查

- 新建 `.cs` 文件后必须调用 `refresh_unity` 触发编译
- 等待编译完成后调用 `read_console` 检查编译错误
- 发现编译错误时立即修复，修复后重新检查
- 编译通过后才能继续创建 `.asset` 资产

---

## 六、ScriptableObject 资产创建规则

### 6.1 优先使用 Unity MCP

- 创建 `.asset` 文件必须优先使用 `manage_scriptable_object(action="create")`
- 传入参数：`type_name`（含命名空间）、`folder_path`、`asset_name`
- 禁止直接使用文件写入工具绕过 MCP 创建 `.asset` 文件

### 6.2 修改 ScriptableObject

- 禁止直接编辑 `.asset` 的 YAML 文件
- 简单字段修改 → 使用 `manage_scriptable_object(action="modify")`
- `[SerializeReference]` 多态字段修改 → 使用 `execute_code` + `SerializedObject` API

### 6.3 回退机制

当 MCP 不可用或创建失败时，允许回退到直接文件写入，但必须说明回退原因：
- MCP 未连接
- 类型不存在
- MCP 创建失败（附具体错误信息）

### 6.4 创建顺序

```
1. 创建 PerkData 资产（如需要）→ 获取引用
2. 创建 EquipmentData 资产（如需要）→ 获取引用
3. 创建 CardData 资产（配置 ID、HeroTypeRestriction、MagicNumbers 基础+升级轨道等全部字段）
4. 设置 CardData 的 EquipmentData 引用（如需要）
5. 设置 CardData 的 CardPerks 中的 PerkData 引用（如需要）
6. 关联卡面图片到 CardData（EquipmentData 不再设置图片）
7. read_console 检查
```

---

## 七、完整创建流程

### 步骤 1：需求收集

向用户收集以下信息（或从用户描述中提取）：

- 卡牌中文名 + 拼音
- 卡牌类型（Attack / Skill / Power / Equipment / Curse / Derived）
- 品质（Starter / Common / Rare / Epic / Corrupted / Special / Unique / DynamicUnique）
- 费用（基础 + 升级后）
- 描述（基础 + 升级后）
- 卡牌目标类型（CardTargetType）— 必须显式确定
- 特性（Traits）
- 打出条件（PlayConditions）
- 魔法数字配置（MagicNumbers：伤害/护甲值、段数、力量倍率、公式）
- 是否临时牌（IsTemp）
- 腐化觉醒配置（仅 Corrupted 品质）

### 步骤 2：效果设计

- 确定基础效果（ManualTargetBaseEffects + OtherEffects）
- 确定升级后效果（UpgradedManualTargetBaseEffects + UpgradedOtherEffects）
- 确定魔法数字配置（MagicNumbers 基础/升级轨道：伤害/护甲值、段数、倍率、公式）
- 检查现有 Effect 是否能满足需求
- 如需新建 Effect → 确认 Effect 名称、参数、对应的 GameAction
- 如需新建 GameAction → 确认 GameAction 名称、所属分类子文件夹

### 步骤 3：Perk 设计（如需要）

- 确定是否需要 Perk
- 确定是内联 CardPerkData（引用已有 PerkData）还是创建新的 PerkData 资产
- 如果创建新 PerkData：
  - 确定存放位置（EquipmentPerk / PowerPerk）
  - 确定 PerkCondition（检查是否需要新建）
  - 确定 BaseEffect 和 TargetMode
  - 确定 IsModifier 模式
- 如果使用内联 CardPerkData：确定 PerkScope（CardTurn / CurrentTurn / Global）

### 步骤 4：EquipmentData 设计（仅 Equipment 类型）

- 确定 AttackDamageTiming
- 确定 Obstruction（渠道限制）
- 确定 EquipEffects / UnEquipEffects / TurnStartEffects / TurnEndEffects
- 确定 EquipmentPerks 引用
- 确定所有 Upgraded* 字段

### 步骤 5：卡面图片查找

- 根据卡牌类型进入 `Sprites/CardFace/{Type}/` 文件夹
- 按卡牌中文名查找匹配的图片
- 如果找到 → 记录图片路径
- 如果未找到 → 列出该文件夹下所有可用图片，向用户询问使用哪张

### 步骤 6：方案确认（必须）

将以上所有收集的信息整理为完整方案，向用户展示：

```
┌─────────────────────────────────────────┐
│           卡牌创建方案确认               │
├─────────────────────────────────────────┤
│ 卡牌名称: XXX（拼音: XXX）              │
│ 类型: XXX  品质: XXX  费用: X(X)        │
│ 描述: XXX                               │
│ 升级描述: XXX                           │
│ 目标类型: XXX                           │
│ 特性: XXX  打出条件: XXX                │
│ 魔法数字: [BasicAttack 6 / 1段 / 无倍率]│
│                                         │
│ 效果:                                   │
│   - 手动目标: XXXEffect(参数)           │
│   - 自动目标: [XXXEffect(参数)]         │
│ 升级效果:                               │
│   - 手动目标: XXXEffect(参数)           │
│   - 自动目标: [XXXEffect(参数)]         │
│                                         │
│ Perk: 是/否                             │
│   - [如是] PerkData: Perk_XXX_XXX      │
│   - Scope: XXX                          │
│                                         │
│ EquipmentData: 是/否                    │
│   - [如是] AttackDamageTiming: XXX     │
│   - Obstruction: XXX                    │
│   - EquipEffects: [XXX]                 │
│                                         │
│ 新建代码: 是/否                         │
│   - [如需] Effect: XXXEffect.cs         │
│   - [如需] GameAction: XXXGa.cs         │
│   - [如需] PerkCondition: XXX.cs        │
│                                         │
│ 卡面图片: XXX.png (已找到/需确认)       │
│                                         │
│ 存储路径:                               │
│   - CardData: Cards/{Type}/Card_XXX_XXX│
│   - EquipmentData: Equipments/Equip_XXX │
│   - PerkData: Perks/{Type}/Perk_XXX_XXX│
└─────────────────────────────────────────┘
```

- 用户确认方案无误 → 进入步骤 7 执行创建
- 用户要求修改 → 根据反馈修改方案 → 重新展示 → 再次确认
- **循环直到用户明确确认后才能开始创建**

### 步骤 7：执行创建

按以下顺序创建（通过 MCP）：

1. 创建新代码文件（如需要）→ `refresh_unity` → `read_console` 检查编译
2. 创建 PerkData 资产（如需要）
3. 创建 EquipmentData 资产（如需要）
4. 创建 CardData 资产
5. 设置引用关系（EquipmentData 引用、PerkData 引用、卡面图片）
6. `read_console` 最终检查

### 步骤 8：验证

- 确认所有资产文件已在正确路径创建
- 确认命名符合规范
- 确认引用关系已正确设置
- 向用户报告创建结果

---

## 八、特殊情况

### 8.1 Derived / Curse 牌

状态牌（Derived）和诅咒牌（Curse）同样走完整流程。虽然它们通常由效果生成，但数据资产仍需按规范创建和配置。

### 8.2 腐化牌（Corrupted）

腐化品质的卡牌需要额外配置 `HasAwaken` 和 `AwakenInsightThreshold`。如果该牌有觉醒效果，`HasAwaken = true` 并设置合理的灵视阈值。

### 8.3 修改器模式 Perk

当 PerkData 的 `IsModifier = true` 时：
- PerkCondition 的 ReactionTiming 会被强制设为 `Pre`
- BaseEffect 应重写 `ModifyAction()` 而非 `GetGameAction()`
- 典型场景：修改伤害目标（如锁链之剑单体变群体）、修改伤害数值
