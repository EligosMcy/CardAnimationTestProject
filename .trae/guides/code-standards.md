# C# 代码规范

## 修改前的脚本检查规则

在修改任何 Unity 脚本前，必须先检查以下项目，并在执行前说明检查结果：

### 命名规范检查

- 类名：PascalCase，与文件名一致
- 私有字段：`_camelCase`（下划线前缀）
- 公有属性：PascalCase
- 公有方法：PascalCase（大写字母开头）
- 私有方法：camelCase（小写字母开头）
- 发现不符合命名规范的方法时，在分析阶段列出，并询问用户是否一并修正

### 架构规范检查

- 是否存在可复用的基类或接口可以继承，避免重复逻辑
- MonoBehaviour 中是否存在不必要的 `Update()` 空方法（若无逻辑则删除）
- 是否有硬编码的魔法数字，应提取为 `[SerializeField] private` 字段或常量

### 性能规范检查

- 是否在 `Update()` 中使用了 `GetComponent<>()`（应缓存到 `Awake()` / `Start()`）
- 是否频繁调用 `FindObjectOfType<>()` 或 `GameObject.Find()`（应避免）
- 字符串拼接是否在热路径上（应改用 StringBuilder 或避免）

---

## 代码风格要求

- 使用 C# 最新语法糖（null 合并、模式匹配、表达式体方法等）
- 注释使用中文，XML 文档注释用于所有方法
- 每次修改只改动与当前需求直接相关的代码，不做无关重构
- 新增代码块前后加空行，保持可读性

### 推荐使用扩展方法

- 当需要为 UnityEngine 或其他第三方类添加通用辅助功能时，优先使用扩展方法

```csharp
public static class TransformExtensions
{
    /// <summary>
    /// 重置变换到初始状态
    /// </summary>
    public static void ResetTransformation(this Transform transform)
    {
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
        transform.position = Vector3.zero;
    }
}
```

---

## 命名空间（Namespace）

### 禁止完全限定类型名

- **禁止** `System.Action<HeroData>`、`System.Collections.Generic.List<int>` 这种内联完全限定名
- **必须** 在文件顶部添加对应的 `using` 声明，然后直接使用短类型名
- using 排序：`System.*` → 第三方库 → 项目命名空间（Data / Enums / Systems / ShowX.Utils 等）

```csharp
// ❌ 错误
System.Action<HeroData> callback = null;

// ✅ 正确
using System;
Action<HeroData> callback = null;
```

---

## 空值检查（Null Safety）

### 禁止 ?. 链式调用

- 完全禁用 `?.` 操作符（包括 `_dataCenter?.Insight?.XXX?.YYY` 这种连续传播）
- 必须在关键位置显式进行 null 判断，并输出错误日志

```csharp
// ❌ 错误
_dataCenter?.Insight?.UpdateValue(value);

// ✅ 正确
if (_dataCenter == null)
{
    LogError("ClassName", "MethodName: _dataCenter is null");
    return;
}
if (_dataCenter.Insight == null)
{
    LogError("ClassName", "MethodName: Insight is null");
    return;
}
_dataCenter.Insight.UpdateValue(value);
```

### Null 判定后必须中断流程

- 发现 null 后必须 `return` / `continue` / `break`，不允许静默跳过继续执行

---

## 命名约定（Naming Convention）

### Public 方法：PascalCase（首字母大写）

```csharp
public void StartNewRun() { }
public void EnterBattle() { }
```

### Private 方法：camelCase（首字母小写）

```csharp
private void handleBattleVictory() { }
private void calculateGoldReward() { }
```

### Private 字段：下划线前缀 `_` + camelCase

```csharp
private RunDataCenter _dataCenter;
private int _maxHealth;
private float _moveSpeed;
```

### 接口：`I` 前缀 + PascalCase

```csharp
public interface IHaveCurrentTargets { }
public interface IDamageable { }
```

### ScriptableObject 类名：`SO` 后缀

```csharp
public class MonsterIntentConfigSO : ScriptableObject { }
public class IntentStepSO : ScriptableObject { }
```

### 按钮回调 / 事件监听方法

- Public 监听方法：`On` + 事件名（PascalCase）
- Private 监听方法：`on` + 事件名（camelCase）

```csharp
// Public — 外部挂载 / UnityEvent 绑定
public void OnClickStart() { }
public void OnHealthChanged() { }

// Private — 内部监听
private void onBattleEnd() { }
private void onCardSelected(CardData card) { }
```

### 常量：ALL_CAPS + 下划线

```csharp
public const int MAX_HAND_SIZE = 10;
private const float DEFAULT_ANIMATION_SPEED = 0.3f;
```

### 公共属性：PascalCase

```csharp
public int CurrentHealth { get; private set; }
public bool IsAlive => _currentHealth > 0;
```

### 枚举类型和枚举值：PascalCase

```csharp
public enum GameFlowState { None, Map, Battle, Shop }
```

### 布尔变量：is / has / can 前缀 + PascalCase

```csharp
// ❌ 错误
public bool dead;
private bool visible;

// ✅ 正确
public bool isDead;
private bool isVisible;
private bool hasShield;
private bool canMove;
```

---

## 方法规范

### 单一职责 + 最大 50 行

- 每个方法只做一件事
- 方法体（含签名和括号）不超过 50 行
- 超过时必须拆分为子方法

### 禁止 `var` 关键字

- 必须显式声明类型，方便 AI 和人类阅读

```csharp
// ❌ 错误
var list = new List<CardData>();
var result = GetComponent<EnemyView>();

// ✅ 正确
List<CardData> list = new List<CardData>();
EnemyView enemyView = GetComponent<EnemyView>();
```

### 禁止布尔参数

- 方法参数中出现 `bool` 时，应拆分为两个语义明确的方法

```csharp
// ❌ 错误
public Vector3 GetTargetPosition(bool worldSpace) { }

// ✅ 正确
public Vector3 GetTargetPositionInWorldSpace() { }
public Vector3 GetTargetPositionInLocalSpace() { }
```

### 禁止魔法数字

- 任何非 `0`/`1`/`-1` 的字面量数字必须定义为命名常量

```csharp
// ❌ 错误
if (currentHealth < 10) { ... }

// ✅ 正确
private const int CRITICAL_HEALTH_THRESHOLD = 10;
if (currentHealth < CRITICAL_HEALTH_THRESHOLD) { ... }
```

---

## 日志规范

### 日志工具：优先 XLogger，Fallback 到 Debug.Log

- 项目中有 XLogger 类时，必须使用 `XLogger.LogXxx("ClassName", "MethodName: 具体信息")` 格式
- 项目中没有 XLogger 时，使用 `Debug.Log` / `Debug.LogWarning` / `Debug.LogError`，格式为 `"[ClassName] MethodName: 具体信息"`
- 不要在每处调用点做 if-else 判断 — 生成代码时根据项目是否存在 XLogger 统一选择

### 日志消息格式

- 统一格式：`"[ClassName] MethodName: 具体信息"`
- 必须包含类名和方法名作为上下文前缀

### 日志级别

项目 XLogger 支持以下 7 个级别（从低到高），可根据实际情况灵活选择：

| 级别              | 使用场景                                                                        |
| ----------------- | ------------------------------------------------------------------------------- |
| **Trace**   | 极细粒度的执行追踪，如方法进入/退出、循环每次迭代（仅用于深度调试，发布前移除） |
| **Debug**   | 开发调试信息，如临时变量值、算法中间状态、条件判断分支走向（发布前可移除）      |
| **Info**    | 正常流程节点确认、数据变更记录、流转/状态切换                                   |
| **Warning** | 可恢复的边缘情况、降级处理的触发、业务规则校验失败                              |
| **Assert**  | 断言失败，如不变量被破坏、内部约定被违反（仅 Debug 环境触发，发布后自动忽略）   |
| **Error**   | 数据异常、空引用、无法恢复的状态、进入不该进入的分支、预期外的值                |
| **Fatal**   | 致命错误，导致程序无法继续运行，如关键资源加载失败、存档损坏、核心系统崩溃      |

---

## 注释规范

### 每个方法必须有 XML 注释

- Public 和 Private 方法都需要 `<summary>` 注释

```csharp
/// <summary>
/// 处理战斗胜利后的奖励结算和状态切换
/// </summary>
private void handleBattleVictory() { }
```

### 每个枚举必须有 XML 注释

- 枚举类型本身需要 `<summary>` 注释说明用途
- 每个枚举值都需要 `<summary>` 注释说明含义

```csharp
/// <summary>
/// 游戏流程状态机，标识当前所处的核心流程阶段
/// </summary>
public enum GameFlowState
{
    /// <summary>未开始 / 初始状态</summary>
    None,
    /// <summary>地图探索阶段</summary>
    Map,
    /// <summary>战斗阶段</summary>
    Battle,
    /// <summary>商店阶段</summary>
    Shop,
}
```

### 成员变量和属性必须有 XML 注释

- 公有属性（Public Property）需要 `<summary>` 注释说明用途和含义
- `[SerializeField]` 私有字段需要 `<summary>` 注释说明用途，便于在 Inspector 中理解
- 普通私有字段建议添加 `<summary>` 注释，说明其存在的目的

```csharp
/// <summary>
/// 当前生命值，对外只读
/// </summary>
public int CurrentHealth { get; private set; }

/// <summary>
/// 最大生命值上限
/// </summary>
public int MaxHealth { get; private set; }

/// <summary>
/// 怪物配置的 ScriptableObject，在 Inspector 中赋值
/// </summary>
[SerializeField] private MonsterConfigSO _monsterConfig;

/// <summary>
/// 数据中心引用，在 Awake 中初始化
/// </summary>
private RunDataCenter _dataCenter;
```

### 复杂逻辑必须写行内注释

- 非自解释的算法、条件判断、位运算等必须注释意图
- 常规注释必须单独成行，禁止无意义的行尾注释
- 允许复杂逻辑使用行内注释说明意图

### 禁止保留注释掉的代码和过时 TODO

- 不要保留注释掉的代码，应直接删除
- 不要保留永远不会完成的 TODO，应直接删除或转化为实际任务

### 代码分区：使用分隔注释

- 使用 `// ==================== 区块名 ====================` 风格
- 不要使用 `#region`（会折叠隐藏代码，不利于 AI 读取上下文）

```csharp
// ==================== Unity 生命周期 ====================
private void Start() { }
private void OnEnable() { }

// ==================== 流程控制 ====================
public void EnterBattle() { }
private void handleBattleVictory() { }

// ==================== 金币 ====================
private void addGold(int amount) { }
```

---

## 文件结构

### 文件内代码顺序（从上到下）

1. using 声明
2. namespace 声明
3. 类声明
4. Fields — 静态字段 → SerializeField 私有字段 → 私有字段 → 公共属性
5. Unity 生命周期 — Awake() → OnEnable() → Start() → Update() → OnDisable() → OnDestroy()
6. Public Methods — 对外接口
7. Private Methods — 内部实现

---

## 异常处理

### 禁止空 catch 块

- 每个 catch 至少输出一条 LogWarning 或 LogError

```csharp
// ❌ 错误
try { DoSomething(); }
catch { }

// ✅ 正确
try { DoSomething(); }
catch (Exception ex)
{
    XLogger.LogError("ClassName", $"DoSomething 失败: {ex.Message}");
}
```
