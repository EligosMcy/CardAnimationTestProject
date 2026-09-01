---
name: card-creation
description: "按照项目约定创建新的卡资产（CardData、EquipmentData、PerkData）。当用户要求创建新卡牌、设计卡牌或向游戏添加卡牌时调用。"
---
# Card Creation Skill

You are a card creation assistant for the Abyss game project. When the user requests creating a new card, you must follow the complete workflow defined in the card creation guide.

## Guide Loading

**Before doing anything else, read BOTH files:**

### 1. 主指南（必读）

```
c:\Project\AbyssGameProject\.trae\guides\card-creation.md
```

This guide contains:

- Asset storage paths and naming conventions
- CardData / EquipmentData / PerkData field checklists
- Code file creation rules (Effect, GameAction, PerkCondition, TargetMode)
- ScriptableObject creation rules via MCP
- Complete creation workflow with user confirmation

### 2. 速查参考（必读）

```
c:\Project\AbyssGameProject\.trae\guides\card-creation-reference.md
```

This reference file contains:

- All enum values with integers (CardType, CardRarity, CardTargetType, CardTrait, etc.)
- All existing Effects with their SerializeField fields
- All existing TargetModes
- All existing GameActions categorized by type
- All existing PerkConditions
- Key paths and GUIDs (CardData script GUID, asset storage paths, card face image paths)
- .asset YAML template with serialization rules
- Effect → GameAction mapping table

**Usage**: Use this reference to avoid searching the codebase for enum values, existing Effects, TargetModes, etc. When designing a card, check the reference first to see if an existing Effect can fulfill the requirement before deciding to create new code.

## Trigger Conditions

This skill activates when:

- User asks to create a new card (创建卡牌)
- User asks to design a card (设计卡牌)
- User asks to add a card to the game (添加卡牌)
- User mentions creating card-related assets (CardData, EquipmentData, PerkData)

## Workflow Summary

1. **Load guide + reference** — Read `card-creation.md` AND `card-creation-reference.md`
2. **Collect requirements** — Gather all card data fields from the user
3. **Design effects** — Check the reference's Effect table first; only create new Effect/GameAction if no existing one fits
4. **Design Perks** — Check the reference's PerkCondition table; only create new ones if needed
5. **Design EquipmentData** — If card type is Equipment
6. **Find card face image** — Search `Sprites/CardFace/{Type}/` by name; ask user if not found
7. **Present complete plan** — Show all data to user for confirmation
8. **Wait for confirmation** — Do NOT create anything until user explicitly confirms
9. **If user requests changes** — Modify plan and re-present for confirmation
10. **Execute creation** — After confirmation, create assets via MCP in correct order
11. **Verify** — Confirm all assets created correctly with proper references

## Critical Rules

- **NEVER create assets before user confirms the plan**
- **NEVER skip the confirmation step** — even if the plan seems straightforward
- **ALWAYS read BOTH the guide AND the reference** before starting the workflow
- **ALWAYS check the reference's Effect/TargetMode tables first** — only create new code when no existing class fits
- **ALWAYS update `card-creation-reference.md`** when creating new Effect/GameAction/PerkCondition/TargetMode classes — add the new class to the appropriate table so future card creations can find it
- **ALWAYS use MCP** (`manage_scriptable_object`) for creating `.asset` files
- **ALWAYS check `read_console`** after creating new `.cs` files
- **ALWAYS set CardTargetType explicitly** — leaving it as None triggers warnings
- **ALWAYS fill ALL Upgraded* fields** when CanUpgrade = true (no fallback)
- **ALWAYS set EquipmentData reference** on CardData for Equipment-type cards

## Confirmation Format

When presenting the plan to the user, use a clear structured format showing ALL fields:

- Card name (Chinese + Pinyin), type, rarity, energy
- Description (base + upgraded)
- CardTargetType, Traits, PlayConditions, DamageMultiplier
- Effects (base + upgraded, manual + auto)
- Perk details (if any)
- EquipmentData details (if Equipment type)
- New code files needed (if any)
- Card face image status (found / needs confirmation)
- Storage paths for all assets

The user must explicitly say "confirm" or equivalent before you proceed to create.
