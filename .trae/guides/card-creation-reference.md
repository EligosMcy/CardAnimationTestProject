# 卡牌创建速查参考

> 本文件为卡牌创建的快速参考表，包含所有枚举值、已有 Effect/TargetMode/GameAction/PerkCondition 清单、关键 GUID 和路径、以及 .asset YAML 模板。
> 创建卡牌时务必先加载本文件，可避免反复搜索代码。

---

## 一、枚举值速查

### CardType（卡牌类型）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| Attack | 0 | 攻击牌 |
| Skill | 1 | 技能牌 |
| Power | 2 | 能力牌 |
| Equipment | 3 | 武器牌（装备后提供战斗效果） |
| Curse | 4 | 诅咒牌 |
| Derived | 5 | 状态牌（无具体类型的负面杂牌，如伤口、灼烧等，不可打出） |

> 源文件：`Assets/Scripts/Enums/CardType.cs`

### CardRarity（品质）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| Starter | 0 | 初始卡 |
| Common | 1 | 普通 |
| Rare | 2 | 稀有 |
| Epic | 3 | 史诗 |
| Corrupted | 4 | 腐化 |
| Special | 5 | 特殊 |
| Unique | 6 | 独特 |
| DynamicUnique | 7 | 动态独特 |

> 源文件：`Assets/Scripts/Enums/CardRarity.cs`

### CardTargetType（目标类型）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| None | 0 | 无目标（必须显式设置，留空触发警告） |
| Self | 1 | 仅作用于玩家自身 |
| Enemy | 2 | 作用于单个敌人 |
| AllEnemy | 3 | 作用于所有敌人 |
| SelfAndEnemy | 4 | 自身 + 一个敌人 |

> 源文件：`Assets/Scripts/Enums/CardTargetType.cs`

### CardTrait（特性）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| Exhaust | 0 | 消耗：打出后进入消耗堆 |
| Ethereal | 1 | 虚无：回合结束未打出则消耗 |
| Innate | 2 | 固有：战斗开始时必定在手牌 |
| Retain | 3 | 保留：回合结束不被弃掉 |
| ReturnToHand | 4 | 返回手牌：打出后回到手牌 |
| Replay | 5 | 重放：打出后自动再打出一次 |

> 源文件：`Assets/Scripts/Enums/CardTrait.cs`
> 注意：Traits 字段序列化为 byte array，空数组 = 无特性（YAML 中为空值），单个特性为 4 字节小端整数

### CardDamageMultiplier（力量倍率）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| None | 0 | 无倍率（×1） |
| Double | 1 | 双倍（×2） |
| Triple | 2 | 三倍（×3） |
| Quadruple | 3 | 四倍（×4） |
| Quintuple | 4 | 五倍（×5） |

> 源文件：`Assets/Scripts/Enums/CardDamageMultiplier.cs`
> **注意**：已不再是 CardData 独立字段（`DamageMultiplier`/`UpgradedDamageMultiplier` 已移除）。当前经 `CardMagicNumberData.StrengthMultiplier`（MagicNumbers 条目）配置，显示层读此处；执行层由 `DealDamageWithMultiplierEffect._strengthMultiplier` 提供（双源一致）

### CardPlayCondition（打出条件）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| None | 0 | 无条件 |
| SoloPlayable | 1 | 孤牌可打 |
| HasEquipmentInExhaust | 2 | 弃牌堆有装备 |
| HasAttackInDrawPile | 3 | 抽牌堆有攻击牌 |
| NeverPlayable | 4 | 永远无法打出 |
| HasAttackInDiscardPile | 5 | 弃牌堆有攻击牌 |
| HasAttackInDrawOrDiscardPile | 6 | 抽/弃牌堆有攻击牌 |
| MinHandSize2 | 7 | 最少2张手牌 |
| MinHandSize3 | 8 | 最少3张手牌 |
| MinHandSize4 | 9 | 最少4张手牌 |
| HasEquippedWeaponThisBattle | 10 | 本场战斗装备过武器牌（读 EquipmentSystem.WeaponsEquippedThisBattle） |
| HasEquippedWeaponThisTurn | 11 | 本回合装备过装备牌（读 EquipmentSystem.WeaponsEquippedThisTurn） |
| HasWeaponInDrawPile | 12 | 抽牌堆有武器牌 |
| HasEquippedWeapon | 13 | 当前已装备武器（读 EquipmentSystem 装备槽占用状态） |
| HasAttackInHandOrDiscardPile | 14 | 手牌或弃牌堆有攻击牌（死魂技艺觉醒条件） |

> 源文件：`Assets/Scripts/Enums/CardPlayCondition.cs`

### CardSpecialEffect（特殊效果标记）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| None | 0 | 无特效 |
| Resistance | 1 | 抗拒 |
| Submit | 2 | 服从（费用-1） |

> 源文件：`Assets/Scripts/Enums/CardSpecialEffect.cs`
> **注意**：该枚举**不是 CardData 字段**，而是 `CardInstance` 的运行时属性（`SetSpecialEffect` 统一入口）。创建卡牌时无需配置；抗拒/服从标记由 `ResistanceContractSystem` / `SubmitWillSystem` 在战斗中赋值。费用修正由全局条件修饰器实时判定：`SubmitCostReductionModifier`（服从-1）、`NonSubmitCostPenaltyModifier`（支配40惩罚，服从牌豁免）

### HeroType（英雄类型限制）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| None | 0 | 未选择/通用（默认） |
| Warrior | 1 | 战士 |
| Mage | 2 | 法师 |
| Assassin | 3 | 刺客 |
| Gunslinger | 4 | 枪手 |

> 源文件：`Assets/Scripts/Enums/HeroType.cs`
> 对应 CardData.HeroTypeRestriction，诅咒/衍生/事件特殊牌应显式设为 None

### CardMagicNumberDataType（魔法数字配置条目类型）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| BasicAttack | 0 | 基础攻击：单段伤害，携带段数基准与力量倍率 |
| BasicArmor | 1 | 基础护甲 |
| BasicSelfDamage | 2 | 基础自伤：给自己扣血（苦痛代偿/悲怆之咒等），对应描述占位符 `{SELF}` |

> 源文件：`Assets/Scripts/Enums/CardMagicNumberType.cs`
> 对应 `CardMagicNumberData.MagicType`（CardData.MagicNumbers 列表中的配置条目，逐条在 Inspector 配置）

### CardMagicNumberType（魔法数字运行时类型）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| Attack | 0 | 伤害，由 BasicAttack 条目展开，对应描述占位符 `{ATK}` |
| Armor | 1 | 护甲，由 BasicArmor 条目展开，对应描述占位符 `{DEF}` |
| HitCount | 2 | 段数，由 BasicAttack 条目展开（HitCountBase 基准/公式求值），对应 `{HIT}` |
| StrengthMultiplier | 3 | 力量倍率，由 BasicAttack 条目展开（显示 1-5 数字），对应 `{MUL}` |
| SoulDevourerCount | 4 | 噬魂匕首全局击杀计数，对应 `{SOUL}` |
| SelfDamage | 5 | 自伤，由 BasicSelfDamage 条目展开，对应描述占位符 `{SELF}` |

> 源文件：`Assets/Scripts/Enums/CardMagicNumberType.cs`
> 配置条目在 CardInstance 构造时一对多展开为运行时条目

### MagicNumberFormulaType（魔法数字公式类型）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| Fixed | 0 | 固定值：返回（基础值, 段数基准） |
| WeaponMultiHit | 1 | 武器多段：有武器 →（基础值, 段数）；无武器 →（基础值, 1）（圆月斩） |
| WeaponBonus | 2 | 武器加成：有武器 →（基础值+加成, 1）；无武器 →（基础值, 1）（横斩） |
| ExhaustWeaponCount | 3 | 消耗堆武器段数：段数 = 1 + 消耗堆武器牌数（秘剑『雨』） |
| LostHealthRatio | 4 | 已损失生命比率：伤害 = 已损失生命 × 比率（返还重斩） |
| GoldRatio | 5 | 金币比率：伤害 = 金币 × 比率（麦德斯之锤） |
| WeaponsEquippedCount | 6 | 武器装备计数：段数 = 本场战斗累计装备武器次数（兵刃怒吼） |
| StackDamage | 7 | 叠加伤害：伤害 = 基础值 + 叠加累计增量（叠加痛击） |
| SoulDevourerDagger | 8 | 噬魂匕首卸下：伤害 = 全局击杀计数 + 额外伤害 |

> 源文件：`Assets/Scripts/Enums/MagicNumberFormulaType.cs`
> 对应 `CardMagicNumberData.Formula`（可空，空 = Fixed）

### CardKeyword（卡牌关键词）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| Discover | 0 | 发现：从牌堆中获取3个卡牌，从中选择一个 |

> 源文件：`Assets/Scripts/Enums/CardKeyword.cs`
> 注意：Keywords 字段在 CardData 中序列化为 List\<CardKeyword\>，支持基础/升级双轨配置
> 显示配置：在 `TraitDisplayConfig.asset` 的 `_keywordEntries` 中配置标题和描述

### CostModifierType（费用修饰器类型）

| 枚举值 | 整数 | 说明 |
|--------|------|------|
| CostReduction | 0 | 通用费用减免（创建 CardCostReductionModifier） |
| ZeroCost | 1 | 单卡本回合费用归零（创建 CardZeroCostModifier） |

> 源文件：`Assets/Scripts/CostModifiers/CardCostReductionModifier.cs`

---

## 二、已有 Effect 速查

> 目录：`Assets/Scripts/Effects/`
> 命名空间：`Effects`
> 基类：`BaseEffect`（命名空间 `Models`）
> 需实现：`GetGameAction(EffectContext context)`，可选重写 `GetBaseDamage(CardInstance)`、`ModifyAction(GameAction)`

| Effect 类名 | SerializeField 字段 | 说明 |
|-------------|---------------------|------|
| AddCardsToLocationEffect | `CardData _cardData, int _count, CardLocation _targetLocation, CardInsertPosition _insertPosition, bool _isUpgraded` | 添加指定卡牌到指定位置（目标位置内的插入位置支持头部/随机/末尾，默认随机；支持创建升级版实例，用于麦德林镰刀装备加伤口入抽牌堆、秘银剑盾卸下加另一形态入抽牌堆） |
| AddEnergyNextTurnEffect | `int _energyAmount` | 下回合获得能量 |
| AddGoldEffect | — | 获得金币 |
| AddInsightEffect | — | 获得灵视 |
| AddRandomWeaponsToDrawPileEffect | `int _count` | 随机武器加入抽牌堆 |
| AddRandomWeaponToHandEffect | `int _count` | 随机武器直接加入手牌（无选择界面） |
| AddStatusEffect | `StatusEffectType _statusEffectType, int _stackCount, StatusSource _source` | 添加状态效果（_source 默认 Card，标识护甲等状态的获取来源） |
| AddStatusSmashEffect | `StatusEffectType _statusType, int _stacks` | 添加状态（粉碎） |
| AddStatusToTargetSmashEffect | `StatusEffectType _statusType, int _stacks` | 对目标添加状态（粉碎） |
| AddVulnerableAndConsumeBuffEffect | `StatusEffectType _buffStatusType, int _stacksToConsume, int _vulnerableStacks` | 为目标附加易伤并消耗自身标记状态（放血剑持有效果：每回合第一张攻击牌为对象附加易伤） |
| AddSoulDevourerDaggerCounterEffect | `int _increment` | 噬魂匕首击杀计数（持有期间每击杀一个生物，全局伤害计数永久+X） |
| AddDragonScaleShieldCounterEffect | `int _increment` | 龙鳞盾技能计数（持有期间每打出技能牌，装备实例计数+X，计数存 CardInstance runtimeData，用于"龙鳞盾"） |
| DragonScaleShieldUnequipEffect | — | 龙鳞盾卸下效果（读取装备实例技能计数，卸下时获得等量护甲，配置在 UnEquipEffects + HeroTM，用于"龙鳞盾"） |
| ApplyVulnerableAndDamageEffect | — | 添加易伤并造成伤害 |
| BallistaEffect | `int _damageAmount` | 弩炮伤害 |
| BloodCoagulationEffect | `int _selfDamage, int _weaponCount, bool _isUpgradedWeapon` | 血凝（自伤+武器数相关，_isUpgradedWeapon 控制发现的武器牌是否以升级状态创建） |
| DiscoverWeaponEffect | `int _discoverCount` | 发现武器牌（从武器牌池随机抽取N张供玩家选择，选择后加入手牌） |
| BloodGreatswordUnequipEffect | `int _selfDamage, int _aoeDamage` | 鲜血大剑卸下效果 |
| BloodStormEffect | `bool _allowChoose` | 血暴 |
| ChangeArtifactIconEffect | — | 更换遗物图标 |
| CurseFutureEffect | `int _hpReduction` | 诅咒未来（扣血） |
| DealDamageBasedOnGoldEffect | `float _goldRatio` | 按玩家当前金币比例造成伤害（不消耗金币） |
| DealDamageBasedOnLostHealthEffect | `float _damageRatio` | 按已损失生命比例伤害 |
| DealDamageEffect | `int _damageAmount, DamageSource _damageSource` | 造成伤害（力量倍率固定 None；_damageSource 默认 Attack，血池喷涌等固定伤害可配 Power） |
| DealDamageWithMultiplierEffect | `int _damageAmount, CardDamageMultiplier _strengthMultiplier` | 带力量倍率的造成伤害（用于"重击"等按倍率享受力量加成的攻击牌；产出 DealDamageGa 携带倍率） |
| DealDamageWithStackEffect | `int _baseDamage, int _stackIncrement, string _stackKey` | 叠层伤害 |
| DeepAbyssWhisperEffect | `int _damageAmount` | 深渊低语伤害 |
| DiscardAttackCardsForCardsEffect | `CardData _cardToGenerate, bool _upgraded` | 消耗攻击牌生成卡 |
| DiscardCardDealDamageEffect | `int _damageAmount, DiscardModeType _discardModeType, int _discardCount` | 弃牌造成伤害 |
| DiscardHandCardEffect | — | 弃手牌 |
| DrawCardAndReduceAttackCostEffect | `int _drawAmount, int _costReduction, bool _setToZero` | 抽牌+攻击牌减费（流转残心） |
| DrawCardEffect | — | 抽牌 |
| DrawWeaponFromDrawPileEffect | `bool _allowChoose` | 从抽牌堆抽武器牌（随机/选择） |
| DrawCardIfDamagedEffect | `int _normalAmount, int _damagedAmount` | 受伤时多抽牌 |
| ExhaustHandRestoreHealthEffect | `int _restorePerCard` | 消耗全部手牌，每张恢复生命 |
| ExhaustHandCardEffect | — | 选择一张手牌消耗（弹出选择界面由玩家指定，用于"食腐鸟"） |
| ExhaustHandCardsGainStrengthEffect | — | 消耗任意数量(0~手牌数)手牌，每张获得1点临时力量 |
| ExhaustDerivedCardsGainStrengthEffect | — | 消耗抽牌堆/手牌/弃牌堆所有衍生牌（CardRarity.Special），每张获得1点永久力量（麦德林镰刀卸下效果，基础/升级共用） |
| ExhaustWeaponMultiHitDamageEffect | `int _damageAmount` | 消耗堆武器多段伤害：对目标造成多段伤害，段数 = 1 + 消耗牌堆中武器牌数量（每张武器牌额外造成一次伤害，用于"秘剑『雨』"） |
| ExpandEquipmentSlotEffect | — | 扩展装备槽 |
| GainEnergyEffect | `int _energyAmount` | 立即获得能量 |
| GainEnergyPerAttackInHandEffect | `int _energyPerAttack` | 按手牌攻击牌数量恢复能量（能量 = 手牌攻击牌数 × 倍率，产出 GrowthEnergyGa；用于"燃魂"） |
| GainArmorBasedOnHandCountEffect | `int _multiplier` | 按手牌数量获得护甲（护甲=手牌数×倍率） |
| GainArmorFromDamageTakenEffect | — | 按受击实际伤害获得等量护甲（Perk 触发，读取 OnDamageTakenGa.ActualDamage；用于血神之咒、鲁莽巨锤） |
| GainStrengthPerWeaponEquippedEffect | `int _strengthPerWeapon` | 按本场战斗装备武器次数获得永久力量（力量 = WeaponsEquippedThisBattle × 每张力量，用于"弁庆之力"） |
| UnequipWeaponReturnHandEffect | — | 卸下 Front 槽位武器并返回手牌（UnEquipGa + EquipmentUnequipDestination.Hand） |
| UnequipWeaponGainEnergyEffect | — | 卸下 Front 槽位武器并放回抽牌堆，获得与该武器初始费用相等的能量（产出 UnequipWeaponGainEnergyGa；用于"收刀入鞘"） |
| HealAlliesSmashEffect | — | 治疗友方（粉碎） |
| KillRestoreEffect | `int _damageAmount, int _healAmount` | 击杀回血 |
| KingVsKingEffect | `int _strengthAmount, int _weakStacks` | 王对王：自己与选中的敌人各+力量，剩余怪物+虚弱 |
| ModifyDealDamageToAoEEffect | — | 修改伤害为群体（修改器模式） |
| MultiHitDamageEffect | `int _damageAmount, int _hitCount` | 多次攻击 |
| MoveDiscardCardToDrawPileTopEffect | `bool _allowChoose` | 将弃牌堆中一张牌放入抽牌堆顶部（true=玩家指定选择，false=随机抽取） |
| MoveDiscardCardsToHandEffect | `int _count` | 从弃牌堆选择指定数量卡牌移回手牌（弹出选择界面由玩家指定，用于"食腐鸟"） |
| RemoveStatusEffect | `StatusEffectType _statusEffectType, int _stackCount` | 移除状态效果 |
| ReduceEquipmentCostEffect | `int _costReduction, string _modifierName` | 武器牌费用减免（磨砺剑刃）：创建全局费用修饰器使所有武器牌费用-N（产出 ApplyCostModifierGa.ForCostReduction，EquipmentCards 作用域 Global 持续） |
| ReplaceAttackCardsEffect | `CardData _cardToGenerate, bool _upgraded` | 替换攻击牌 |
| ReplayAttackCardsEffect | — | 重放攻击牌 |
| RestoreHealthEffect | `int _restoreAmount` | 恢复生命 |
| ResurrectEnemyEffect | `ResurrectMode _resurrectMode, int _resurrectHealth, int _newMaxHealth` | 复活敌人 |
| ResurrectHeroEffect | `ResurrectMode _resurrectMode, int _resurrectHealth, int _newMaxHealth` | 复活英雄 |
| SelfDamageEffect | `int _damageAmount` | 自伤（支付生命代价） |
| ShowCardRewardEffect | — | 显示卡牌奖励 |
| SoulDevourerDaggerUnequipEffect | `int _bonusDamage` | 噬魂匕首卸下效果（对全体敌人造成 计数+额外伤害） |
| StealCardSmashEffect | — | 偷取卡牌（粉碎） |
| StealGoldSmashEffect | — | 偷取金币（粉碎） |
| ToxinEffect | `int _damagePerTurn` | 毒素每回合伤害 |
| TransformDrawPileCardsEffect | `CardData _cardToGenerate, int _count, bool _upgraded, bool _allowChoose` | 转化抽牌堆N张牌为指定卡牌（随机/指定选择） |
| TriggerBurnEffect | — | 触发燃烧 |
| TriggerPoisonEffect | — | 触发中毒 |
| TurnStartSelfDamageEffect | — | 回合开始自伤 |
| WeaponRecastEffect | — | 武器重铸 |
| WeaponsEquippedDamageEffect | `int _damageAmount` | 按本场战斗装备武器次数造成多段单体伤害（段数 = EquipmentSystem.WeaponsEquippedThisBattle，用于"兵刃怒吼"） |
| WeaponBonusDamageEffect | `int _baseDamage, int _weaponBonusDamage` | 武器加成伤害：造成基础伤害，若已装备武器（EquipmentSystem.HasEquippedWeapon）则额外附加武器加成伤害（用于"横斩"） |
| WeaponMultiHitDamageEffect | `int _baseDamage, int _weaponHitCount` | 武器多段伤害：造成基础伤害，若已装备武器（EquipmentSystem.HasEquippedWeapon）则攻击次数变为 _weaponHitCount 次（用于"圆月斩"） |

---

## 三、已有 TargetMode 速查

> 目录：`Assets/Scripts/TargetModels/`
> 命名空间：`TargetModels`
> 基类：`TargetMode`（命名空间 `Models`）

| TargetMode 类名 | 说明 |
|------------------|------|
| AllEnemiesTM | 全体敌人 |
| HeroTM | 玩家英雄（自身） |
| ManualTM | 手动选择目标 |
| ManualTargetsTM | 多目标手动模式（持有多个手动选定目标列表，与 ManualTM 区别：支持目标列表，用于 UseCurrentTargetAsTarget 等需整体传递多目标的场景） |
| NoTM | 无目标 |
| RandomEnemyTM | 随机敌人 |

---

## 四、已有 GameAction 速查

> 目录：`Assets/Scripts/GameActions/`
> 命名空间：`GameActions`（所有 Ga 统一平铺在此命名空间，子文件夹仅为文件组织）

### Base（基础动作）

| GameAction 类名 | 说明 |
|------------------|------|
| AddGoldGa | 获得金币 |
| AddInsightGa | 获得灵视 |
| AddRandomWeaponsToDrawPileGa | 随机武器加入抽牌堆 |
| ApplyCostModifierGa | 应用费用修饰器（创建上下文，静态工厂 ForZeroCost / ForCostReduction） |
| ApplyVulnerableAndDamageGa | 易伤并伤害 |
| BloodCoagulationGa | 血凝（自伤+发现武器选择+回合结束卸下，IsUpgradedWeapon 控制武器升级状态） |
| BloodGreatswordUnequipGa | 鲜血大剑卸下 |
| CheckKillRestoreGa | 检查击杀回血 |
| CheckWhisperReplayGa | 检查低语重放 |
| DealDamageBasedOnLostHealthGa | 按已损失生命比例伤害 |
| DealDamageGa | 造成伤害 |
| DealDamageWithStackGa | 叠层伤害 |
| DeepAbyssWhisperGa | 深渊低语 |
| DiscardCardDealDamageGa | 弃牌伤害 |
| DrawCardAndReduceAttackCostGa | 抽牌+攻击牌减费 |
| GainArmorBasedOnHandCountGa | 按手牌数量获得护甲 |
| KillRestoreDamageGa | 击杀回血伤害 |
| MonsterDamageIntentGa | 怪物伤害意图 |
| MultiHitDamageGa | 多次攻击 |
| RestoreHealthGa | 恢复生命 |
| SelfDamageGa | 自伤 |
| ShowCardRewardGa | 显示卡牌奖励 |
| TurnStartSelfDamageGa | 回合开始自伤 |

### Card（卡牌相关）

| GameAction 类名 | 说明 |
|------------------|------|
| AddRandomWeaponToHandGa | 随机武器加入手牌 |
| AddCardsToLocationGa | 添加指定卡牌到指定位置（数量/卡牌/位置参数化） |
| AutoPlayCardGa | 自动打出卡牌 |
| BloodStormGa | 血暴 |
| CardPlayedGa | 卡牌打出 |
| CurseFutureGa | 诅咒未来 |
| DiscardAttackCardsForCardsGa | 消耗攻击牌生成卡 |
| DiscardCardGa | 弃牌 |
| DiscardHandCardGa | 弃手牌 |
| DiscoverWeaponGa | 发现武器（从武器牌池随机抽取N张供选择，选择后加入手牌） |
| DrawWeaponFromDrawPileGa | 从抽牌堆抽武器牌 |
| ExhaustHandRestoreHealthGa | 消耗手牌恢复生命 |
| ExhaustHandCardGa | 选择一张手牌消耗（弹出选择界面由玩家指定，用于"食腐鸟"） |
| ExhaustHandCardsGainStrengthGa | 消耗任意数量手牌获得临时力量 |
| ExhaustDerivedCardsGainStrengthGa | 消耗三堆衍生牌获得永久力量 |
| DrawCardsGA | 抽牌 |
| EndTurnDiscardCardsGa | 回合结束弃牌 |
| ExhaustCardGa | 消耗卡牌 |
| GainStrengthPerWeaponEquippedGa | 按武器装备次数获得力量（力量 = 本场装备武器次数 × 每张力量，Performer 在 CardSystem） |
| PlayCardGa | 打出卡牌 |
| ReplaceAttackCardsGa | 替换攻击牌 |
| ReplayAttackCardsGa | 重放攻击牌 |
| ReplaySingleAttackGa | 重放单张攻击牌 |
| ReturnCardToHandGa | 卡牌返回手牌 |
| MoveDiscardCardToDrawPileTopGa | 将弃牌堆一张牌放入抽牌堆顶部（随机抽取/指定选择） |
| MoveDiscardCardsToHandGa | 从弃牌堆选择指定数量卡牌移回手牌（玩家指定选择，用于"食腐鸟"） |
| TransformDrawPileCardsGa | 转化抽牌堆卡牌 |
| WeaponRecastGa | 武器重铸 |

### Combat（战斗相关）

| GameAction 类名 | 说明 |
|------------------|------|
| DealDamageBasedOnGoldGa | 按当前金币比例造成伤害（不消耗金币） |
| OnDamageTakenGa | 受击 |
| OnFullBlockGa | 完全格挡 |
| OnUnitTookRealDamageGa | 单位受到真实伤害事件（任意单位实际扣血 realDamage > 0 时发射，供订阅者响应，用于血迹吸收等） |

### Effect（效果相关）

| GameAction 类名 | 说明 |
|------------------|------|
| AddStatusEffectGa | 添加状态效果 |
| ConsumeNextAttackPowerGa | 消耗下次攻击力 |
| KingVsKingGa | 王对王（自己与选中敌人+力量，剩余怪物+虚弱） |
| PerformEffectGa | 执行效果 |
| RemoveStatusEffectGa | 移除状态效果 |
| TriggerBurnGa | 触发燃烧 |
| TriggerPoisonGa | 触发中毒 |
| TriggerTightenGa | 触发紧缚 |

### Enemy（敌人相关）

| GameAction 类名 | 说明 |
|------------------|------|
| AttackHeroGa | 攻击英雄 |
| BallistaGa | 弩炮 |
| KillEnemyGa | 击杀敌人 |
| ResurrectEnemyGa | 复活敌人 |
| SacrificeEnemyGa | 献祭敌人 |
| StealCardGa | 偷取卡牌 |
| StealGoldGa | 偷取金币 |

### Energy（能量相关）

| GameAction 类名 | 说明 | 处理系统 |
|------------------|------|----------|
| AddEnergyNextTurnGa | 下回合获得能量 | EnergySystem |
| GrowthEnergyGa | 立即增加能量 | EnergySystem |
| RefillEnergyGa | 恢复能量到上限 | EnergySystem |
| SpendEnergyGa | 消耗能量 | EnergySystem |

### Equipment（装备相关）

| GameAction 类名 | 说明 |
|------------------|------|
| AddSoulDevourerDaggerCounterGa | 噬魂匕首击杀计数 |
| AddDragonScaleShieldCounterGa | 龙鳞盾技能计数（增加装备实例技能计数，performer 在 StatusEffectSystem） |
| DragonScaleShieldUnequipGa | 龙鳞盾卸下（读取实例计数，添加等量护甲并清零，performer 在 StatusEffectSystem） |
| EquipGa | 装备 |
| ExpandEquipmentSlotGa | 扩展装备槽 |
| ReduceEquipmentSlotGa | 减少装备槽 |
| SoulDevourerDaggerUnequipGa | 噬魂匕首卸下 |
| UnequipGa | 卸下装备 |
| UnequipWeaponGainEnergyGa | 卸下 Front 槽位武器并放回抽牌堆，获得与武器初始费用相等的能量（收刀入鞘） |

### Hero（英雄相关）

| GameAction 类名 | 说明 |
|------------------|------|
| KillHeroGa | 英雄死亡 |
| ResurrectHeroGa | 复活英雄 |

### Artifact（遗物相关）

| GameAction 类名 | 说明 |
|------------------|------|
| ArtifactGa | 遗物 |
| ExchangeArtifactGa | 交换遗物 |
| RemoveArtifactGa | 移除遗物 |

### logic（战斗流程）

| GameAction 类名 | 说明 |
|------------------|------|
| CheckBattleEndGa | 检查战斗结束 |
| CombatPreparationGa | 战斗准备 |
| EndBattleGa | 结束战斗 |
| EnemyTurnEndGa | 敌人回合结束 |
| EnemyTurnGa | 敌人回合 |
| EnemyTurnStartGa | 敌人回合开始 |
| LostGa | 失败 |
| PlayerTurnEndGa | 玩家回合结束 |
| PlayerTurnGa | 玩家回合 |
| PlayerTurnStartGa | 玩家回合开始 |
| StartBattleGa | 开始战斗 |
| WonGa | 胜利 |

---

## 五、已有 CostModifier 速查

> 目录：`Assets/Scripts/CostModifiers/`
> 命名空间：`CostModifiers`
> 接口：`ICostModifier`（含 `Name`）+ `ITurnBaseCostModifier`（Duration/RemainingTurns/TickTurn）
> 创建：常规修饰器**必须**通过 `ApplyCostModifierGa` 动作派发，由 `CostModifierApplier` 统一创建并注册；外界禁止直接调用 `CostReductionSystem.RegisterModifier`。
> **例外**：手牌被动机制（`CardPassiveCostSystem`）在战斗开始时直接调用 `CostReductionSystem.RegisterModifier` 注册 `AttackFollowUpCostReductionModifier`（不走 ApplyCostModifierGa 管线）；`BloodRepayCostReductionModifier` 为实例级修饰器，通过 `CardInstance.AddInstanceModifier` 添加（作用域单卡，与全局修饰器链并存）。
> 类型：由 `CostModifierType` 枚举显式区分，通过静态工厂指定：
> - `ApplyCostModifierGa.ForZeroCost(name, targetCard)` → 创建 `CardZeroCostModifier`（单卡本回合归零）
> - `ApplyCostModifierGa.ForCostReduction(name, costReduction, scope, duration, turns)` → 创建 `CardCostReductionModifier`（卡牌费用减免）
> 回合衰减由 `CostReductionSystem` 统一驱动

| CostModifier 类名 | 说明 |
|-------------------|------|
| CardCostReductionModifier | 卡牌费用减免（CostReduction 类型，AllCards/EquipmentCards 作用域，Duration 支持 Global/TurnBased） |
| CardZeroCostModifier | 单卡本回合费用归零（ZeroCost 类型，绑定 CardInstance，Duration=TurnBased，RemainingTurns=1） |
| AttackFollowUpCostReductionModifier | 攻击牌后条件减费（全局修饰器，绑定 CardData，ModifyCost 时查询 `CardSystem.LastPlayedCard` 为攻击牌则 -1；战斗开始由 CardPassiveCostSystem 注册，战斗结束由 CostReductionSystem 清空） |
| BloodRepayCostReductionModifier | 鲜血偿还受伤减费（实例级修饰器，固定 -1 无条件；英雄受真实伤害时由 CardPassiveCostSystem 添加到手牌中「鲜血偿还」实例，打出后由 RemoveInstanceModifier 移除） |
| SubmitCostReductionModifier | 服从牌减费（全局条件修饰器，CardInstance.SpecialEffect == Submit 时费用 -1；由 CardPassiveCostSystem 注册） |
| NonSubmitCostPenaltyModifier | 非服从牌惩罚（全局条件修饰器，支配40 触发时对 SpecialEffect != Submit 的牌加费，服从牌豁免；由 DominatorSystem 注册） |

---

## 六、已有 PerkCondition 速查

> 目录：`Assets/Scripts/PerkConditions/`
> 命名空间：`PerkConditions`
> 基类：`PerkCondition`（命名空间 `Models`）
> 需实现：`SubscribeCondition()`、`UnSubscribeCondition()`、`SatisfiesConditionIsMet()`

| PerkCondition 类名 | 说明 |
|---------------------|------|
| OnAttackPlayedCondition | 攻击牌打出时 |
| OnAttackPlayedWithStatusCondition | 带状态攻击牌打出时 |
| OnArmorGainedFromCardCondition | 从卡牌获取护甲值时（监听 PerformEffectGa Post，效果来源为卡牌且为 AddStatusEffect 护甲；用于秘银剑盾-盾形态） |
| OnBossPhaseOneDefeatedCondition | Boss一阶段击败时 |
| OnCardPlayedCondition | 卡牌打出时 |
| OnCombatStartCondition | 战斗开始时 |
| OnDamageTakenCondition | 单位受到伤害时（监听 OnDamageTakenGa，可读取 ActualDamage 实际伤害值） |
| OnDiscardCondition | 弃牌时 |
| OnEnemyDiedCondition | 敌人死亡时 |
| OnEnemyDamageTakenCondition | 敌人受到伤害时（监听 OnDamageTakenGa，受击目标为敌人且伤害来源为英雄；用于秘银剑盾-剑形态） |
| OnEnemyResurrectedCondition | 敌人复活时 |
| OnEquipCondition | 装备时 |
| OnEquipmentPlayedCondition | 装备牌打出时 |
| OnEquipOrUnequipCondition | 装备或卸下时 |
| OnFullBlockCondition | 完全格挡时 |
| OnHealedCondition | 治疗时 |
| OnHeroDiedCondition | 英雄死亡时 |
| OnHeroResurrectedCondition | 英雄复活时 |
| OnPowerPlayedCondition | 能力牌打出时 |
| OnSelfDamageCondition | 自伤时 |
| OnSkillPlayedCondition | 技能牌打出时 |
| OnTriggerBurnCondition | 触发燃烧时 |
| OnTriggerPoisonCondition | 触发中毒时 |
| OnTurnEndCondition | 回合结束时 |
| OnTurnStartCondition | 回合开始时 |
| OnUnequipCondition | 卸下装备时 |
| OnUnitTookRealDamageCondition | 任意单位受到真实伤害时（监听 OnUnitTookRealDamageGa，可读受伤单位与真实伤害值；用于血迹吸收） |

---

## 七、关键路径与 GUID

### 脚本 GUID

| 脚本 | GUID | 用途 |
|------|------|------|
| CardData.cs | `76ff972979bea0a48b275cfd92b2e098` | .asset 文件的 m_Script 引用（源文件 `Assets/Scripts/Data/CardData.cs`） |

### 资产存储路径

| 资产类型 | 根路径 | 命名格式 |
|----------|--------|----------|
| CardData | `Assets/Resources/Data/FirstMonthResources/Cards/{Type}/` | `{ID}_Card_{拼音}_{中文名}.asset` |
| EquipmentData | `Assets/Resources/Data/FirstMonthResources/Equipments/` | `Equip_{拼音}_{中文名}.asset` |
| PerkData | `Assets/Resources/Data/FirstMonthResources/Perks/{Type}Perk/` | `Perk_{卡牌中文名}_{效果中文名}.asset` |

> CardData 命名中的 `{ID}` 为卡牌全局唯一 ID（对应 `Assets/CardGameAssets/表/Data/#Card.xlsx`），如 `10001_Card_ZhanJi_斩击.asset`

### 卡面图片路径

| 路径 | 说明 |
|------|------|
| `Assets/Sprites/CardFace/{Type}/` | 按类型分子文件夹，文件名格式 `{类型}_{中文名}.png`（如 `攻击_斩击.png`、`技能_格挡.png`、`能力_狂血形态.png`、`武器_见切太刀.png`） |

### 代码目录

| 目录 | 路径 | 用途 |
|------|------|------|
| Effects | `Assets/Scripts/Effects/` | 所有 Effect 类 |
| TargetModels | `Assets/Scripts/TargetModels/` | 所有 TargetMode 类 |
| GameActions | `Assets/Scripts/GameActions/{Category}/` | 所有 GameAction 类 |
| PerkConditions | `Assets/Scripts/PerkConditions/` | 所有 PerkCondition 类 |
| Enums | `Assets/Scripts/Enums/` | 所有枚举定义 |
| Systems | `Assets/Scripts/Systems/Battle/` | 战斗系统（验证 GameAction 处理器） |

### 参考卡牌资产（YAML 格式参照）

| 卡牌 | 路径 | 参考点 |
|------|------|--------|
| 斩击 | `Cards/Attack/10001_Card_ZhanJi_斩击.asset` | 标准攻击牌：ManualTargetBaseEffects（复数 rid 列表）+ MagicNumbers（BasicAttack 6伤害）+ 描述 `{ATK}` 占位符 |
| 重击 | `Cards/Attack/10022_Card_ZhongJi_重击.asset` | DealDamageWithMultiplierEffect（力量倍率）配置参考 |
| 圆月斩 | `Cards/Attack/10056_Card_YuanYueZhan_圆月斩.asset` | MagicNumbers Formula=WeaponMultiHit（武器多段）参考 |
| 连续重斩 | `Cards/Attack/10068_Card_LianXuZhongZhan_连续重斩.asset` | 攻击牌后条件减费绑定卡（CardSystem.LastPlayedCard 为攻击牌时费用 -1，由 AttackFollowUpCostReductionModifier 实现） |
| 鲜血偿还 | `Cards/Attack/10069_Card_XianXueChangHuan_鲜血偿还.asset` | 受伤减费卡（英雄受真实伤害时手牌中实例 -1 费，由 BloodRepayCostReductionModifier + CardPassiveCostSystem 实现） |

---

## 八、.asset YAML 模板

> 以下为通用 CardData .asset 模板，`{...}` 为占位符需替换。
> 此模板展示 ManualTargetBaseEffects（复数）+ 1 个 OtherEffects（TargetMode + BaseEffect）+ MagicNumbers 的基础+升级配置。

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 76ff972979bea0a48b275cfd92b2e098, type: 3}
  m_Name: "{ID}_Card_{Pinyin}_{Unicode中文名}"
  m_EditorClassIdentifier:
  <ID>k__BackingField: {ID整数}
  <HeroTypeRestriction>k__BackingField: {HeroType整数}
  <CardType>k__BackingField: {CardType整数}
  <Title>k__BackingField: "{Unicode中文名}"
  <Description>k__BackingField: "{Unicode描述，动态数值用占位符 {ATK}/{DEF}/{HIT}/{MUL}/{SOUL}/{SELF}}"
  <Rarity>k__BackingField: {Rarity整数}
  <IsTemp>k__BackingField: 0
  <Energy>k__BackingField: {费用}
  <Image>k__BackingField: {fileID: 21300000, guid: {图片GUID}, type: 3}
  <ManualTargetBaseEffects>k__BackingField:
  - rid: {rid1}
  - rid: {rid2}
  <OtherEffects>k__BackingField:
  - <TargetMode>k__BackingField:
      rid: {rid3}
    <BaseEffect>k__BackingField:
      rid: {rid4}
  <MagicNumbers>k__BackingField:
  - <MagicType>k__BackingField: {0=BasicAttack, 1=BasicArmor, 2=BasicSelfDamage}
    <BaseValue>k__BackingField: {伤害/护甲值}
    <HitCountBase>k__BackingField: {段数基准, 默认1}
    <Formula>k__BackingField:
      _type: {0=Fixed, 1=WeaponMultiHit, 2=WeaponBonus, ... 见 MagicNumberFormulaType 枚举}
      _weaponHitCount: 0
      _bonusValue: 0
      _ratio: 0
    <StrengthMultiplier>k__BackingField: {0=None, 1=Double, 2=Triple, 3=Quadruple, 4=Quintuple}
  <CardPerks>k__BackingField: []
  <Traits>k__BackingField:
  <PlayConditions>k__BackingField:
  <Keywords>k__BackingField:
  <CardTargetType>k__BackingField: {CardTargetType整数}
  <CanUpgrade>k__BackingField: 1
  <UpgradedTitle>k__BackingField: "{Unicode升级标题（基础标题+，如 斩击+）}"
  <UpgradedDescription>k__BackingField: "{Unicode升级描述}"
  <UpgradedEnergy>k__BackingField: {升级后费用}
  <UpgradedManualTargetBaseEffects>k__BackingField:
  - rid: {rid5}
  <UpgradedOtherEffects>k__BackingField: []
  <UpgradedMagicNumbers>k__BackingField:
  - <MagicType>k__BackingField: {0=BasicAttack, 1=BasicArmor, 2=BasicSelfDamage}
    <BaseValue>k__BackingField: {升级后伤害/护甲值}
    <HitCountBase>k__BackingField: 1
    <Formula>k__BackingField:
      _type: 0
      _weaponHitCount: 0
      _bonusValue: 0
      _ratio: 0
    <StrengthMultiplier>k__BackingField: 0
  <UpgradedCardPerks>k__BackingField: []
  <UpgradedTraits>k__BackingField:
  <UpgradedPlayConditions>k__BackingField:
  <UpgradedKeywords>k__BackingField:
  <UpgradedCardTargetType>k__BackingField: {升级后CardTargetType整数}
  <HasAwaken>k__BackingField: 0
  <AwakenInsightThreshold>k__BackingField: 80
  <EquipmentData>k__BackingField: {fileID: 0}
  references:
    version: 2
    RefIds:
    - rid: {rid1}
      type: {class: {Effect类名}, ns: Effects, asm: Assembly-CSharp}
      data:
        {字段名}: {值}
    # ... 其余 rid 同理：
    # - ManualTargetBaseEffects 的每个元素是 1 个 Effect rid
    # - AutoTargetEffect 是 TargetMode + BaseEffect 两个 rid 成对
    # - 升级轨道使用独立 rid
```

### YAML 序列化规则

1. **中文字符**：使用 `\uXXXX` Unicode 转义（如 `苦` = `\u82E6`）
2. **rid 值**：使用大整数（如 `7000000000000000001`），同一文件内唯一即可
3. **空 List**：使用 `[]`（如 `OtherEffects: []`、`CardPerks: []`）
4. **空数组字段**（Traits / PlayConditions / Keywords）：字段名后直接换行，不写值
5. **ManualTargetBaseEffects**：纯 Effect rid 列表，每个元素一个 rid（对应 references 中的一个 Effect 条目）
6. **AutoTargetEffect**：`TargetMode` + `BaseEffect` 两个 rid 成对
7. **MagicNumbers 条目**：每条目含 `MagicType` / `BaseValue` / `HitCountBase` / `Formula`（`_type`/`_weaponHitCount`/`_bonusValue`/`_ratio` 四字段，未配置时 `_type: 0`=Fixed）/ `StrengthMultiplier`
8. **HeroTM 无 data 字段**：`data:` 后直接换行
9. **.meta 文件**：每个 .asset 需配套 .meta 文件，包含唯一 GUID

### .meta 文件模板

```yaml
fileFormatVersion: 2
guid: {新生成的GUID}
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
```

---

## 九、Effect → GameAction 映射（常见）

> 创建新 Effect 时需确认对应 GameAction 已有系统处理器。
> 若 GameAction 不存在，需同时新建 Effect + GameAction。

| Effect | GameAction | 处理系统 |
|--------|------------|----------|
| SelfDamageEffect | SelfDamageGa | — |
| DealDamageEffect | DealDamageGa | — |
| DealDamageWithMultiplierEffect | DealDamageGa | — |
| GainEnergyEffect | GrowthEnergyGa | EnergySystem |
| GainEnergyPerAttackInHandEffect | GrowthEnergyGa | EnergySystem |
| AddEnergyNextTurnEffect | AddEnergyNextTurnGa | EnergySystem |
| DrawCardEffect | DrawCardsGA | — |
| DrawCardAndReduceAttackCostEffect | DrawCardAndReduceAttackCostGa | — |
| RestoreHealthEffect | RestoreHealthGa | — |
| AddGoldEffect | AddGoldGa | — |
| DealDamageBasedOnGoldEffect | DealDamageBasedOnGoldGa | DamageSystem |
| TransformDrawPileCardsEffect | TransformDrawPileCardsGa | CardSystem |
| AddInsightEffect | AddInsightGa | — |
| AddVulnerableAndConsumeBuffEffect | AddStatusEffectGa | StatusEffectSystem |
| AddRandomWeaponsToDrawPileEffect | AddRandomWeaponsToDrawPileGa | — |
| AddRandomWeaponToHandEffect | AddRandomWeaponToHandGa | CardSystem |
| DiscoverWeaponEffect | DiscoverWeaponGa | CardSystem |
| ExhaustHandCardsGainStrengthEffect | ExhaustHandCardsGainStrengthGa | CardSystem |
| GainStrengthPerWeaponEquippedEffect | GainStrengthPerWeaponEquippedGa | CardSystem |
| ReduceEquipmentCostEffect | ApplyCostModifierGa | CostReductionSystem |
| UnequipWeaponGainEnergyEffect | UnequipWeaponGainEnergyGa | EquipmentSystem |
| KingVsKingEffect | KingVsKingGa | StatusEffectSystem |

> 标注 `—` 的处理系统未在此速查中列出，需按需搜索 `Assets/Scripts/Systems/` 确认。
