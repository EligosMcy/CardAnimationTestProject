# 系统亮点与参数配置详解

## 1. 系统亮点总览

```
┌─────────────────────────────────────────────────────────────────┐
│                    UiCard 系统亮点                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. 全参数化配置系统     → ScriptableObject 驱动               │
│  2. 状态机驱动架构       → 清晰的行为分离                      │
│  3. 双通道排序机制       → Z 轴 + SortingOrder                 │
│  4. 平滑运动插值         → Lerp 线性插值动画                   │
│  5. 双玩家自动适配       → 上下边缘检测                        │
│  6. 事件驱动松耦合       → Observer 模式                       │
│  7. 可扩展架构           → 易于添加新状态和功能               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## 2. 参数配置系统

### 2.1 UiCardParameters 类

**文件**：[UiCardParameters.cs](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardParameters/UiCardParameters.cs)

```csharp
[CreateAssetMenu(menuName = "Card Config Parameters")]
public class UiCardParameters : ScriptableObject
{
    // 所有参数都可在 Inspector 中实时调整
}
```

**使用方式**：
1. 在 Project 窗口右键 `Create > Card Config Parameters`
2. 创建配置资产
3. 拖拽到卡牌预制体的 `UiCardHandComponent` 组件上

### 2.2 参数分组

#### Disable 参数

| 参数 | 类型 | 范围 | 默认值 | 说明 |
|------|------|------|--------|------|
| `DisabledAlpha` | float | 0.1~1 | 0.5 | 禁用时卡牌透明度 |

#### Hover 参数

| 参数 | 类型 | 范围 | 默认值 | 说明 |
|------|------|------|--------|------|
| `HoverHeight` | float | 0~4 | 1 | Hover 上移距离 |
| `HoverRotation` | bool | - | false | 是否保留原旋转 |
| `HoverScale` | float | 0.9~2 | 1.3 | Hover 缩放倍数 |
| `HoverSpeed` | float | 0~25 | 15 | Hover 动画速度 |

#### Bend 参数

| 参数 | 类型 | 范围 | 默认值 | 说明 |
|------|------|------|--------|------|
| `Height` | float | 0~1 | 0.12 | 高度因子 |
| `Spacing` | float | 0~5 | 2 | 卡牌间距 |
| `BentAngle` | float | 0~60 | 20 | 总弯曲角度 |

#### Movement 参数

| 参数 | 类型 | 范围 | 默认值 | 说明 |
|------|------|------|--------|------|
| `RotationSpeed` | float | 0~60 | 20 | 旋转速度（玩家） |
| `RotationSpeedP2` | float | 0~1000 | 500 | 旋转速度（对手） |
| `MovementSpeed` | float | 0~15 | 4 | 移动速度 |
| `ScaleSpeed` | float | 0~15 | 7 | 缩放速度 |

#### Draw/Discard 参数

| 参数 | 类型 | 范围 | 默认值 | 说明 |
|------|------|------|--------|------|
| `StartSizeWhenDraw` | float | 0~1 | 0.05 | 抽牌起始缩放 |
| `DiscardedSize` | float | 0~1 | 0.5 | 弃牌目标缩放 |

### 2.3 默认配置

```csharp
// UiCardParameters.cs#L142-L163
[Button]
public void SetDefaults()
{
    disabledAlpha = 0.5f;
    
    hoverHeight = 1;
    hoverRotation = false;
    hoverScale = 1.3f;
    hoverSpeed = 15f;
    
    height = 0.12f;
    spacing = -2;  // 注意：内部存储为负值，访问器返回正值
    bentAngle = 20;
    
    rotationSpeedP2 = 500;
    rotationSpeed = 20;
    movementSpeed = 4;
    scaleSpeed = 7;
    
    startSizeWhenDraw = 0.05f;
    discardedSize = 0.5f;
}
```

## 3. 状态机架构亮点

### 3.1 状态栈设计

```csharp
// BaseStateMachine 提供 Push/Pop 机制
public void PushState<T>() where T : IState
{
    // 压入新状态
}

public void PopState()
{
    // 弹出当前状态，恢复上一个
}
```

**状态转换图**：

```
┌─────────────────────────────────────────────────────────────┐
│                    状态栈管理                               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  初始状态：                                                 │
│  ┌─────────┐                                               │
│  │   Idle   │                                               │
│  └─────────┘                                               │
│                                                             │
│  鼠标悬停：                                                 │
│  ┌─────────┐                                               │
│  │  Hover  │  ← PushState<UiCardHover>()                   │
│  ├─────────┤                                               │
│  │   Idle   │                                               │
│  └─────────┘                                               │
│                                                             │
│  鼠标离开：                                                 │
│  ┌─────────┐                                               │
│  │   Idle   │  ← PopState() 恢复                           │
│  └─────────┘                                               │
│                                                             │
│  点击选中：                                                 │
│  ┌─────────┐                                               │
│  │   Drag   │  ← PushState<UiCardDrag>()                   │
│  ├─────────┤                                               │
│  │  Hover  │                                               │
│  ├─────────┤                                               │
│  │   Idle   │                                               │
│  └─────────┘                                               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 状态独立封装

每个状态类独立封装行为：

```csharp
// UiCardIdle.cs - 只处理 Idle 状态的逻辑
public class UiCardIdle : UiBaseCardState
{
    public override void OnEnterState() { /* 进入 Idle */ }
    public override void OnExitState()  { /* 离开 Idle */ }
    public override void OnUpdate()     { /* Idle 每帧更新 */ }
}

// UiCardHover.cs - 只处理 Hover 状态的逻辑
public class UiCardHover : UiBaseCardState
{
    public override void OnEnterState() { /* 进入 Hover */ }
    public override void OnExitState()  { /* 离开 Hover */ }
}
```

**好处**：
- 单一职责，每个状态只关注自身行为
- 易于测试和调试
- 添加新状态不影响现有代码

### 3.3 状态检查保护

```csharp
// 防止在错误状态下响应事件
private void OnPointerEnter(PointerEventData obj)
{
    if (Fsm.IsCurrent(this))  // 必须当前处于 Idle 状态
        Handler.Hover();
}
```

**保护机制**：
- 每个事件处理都检查 `Fsm.IsCurrent(this)`
- 避免在状态转换过程中触发错误行为

## 4. 运动插值系统亮点

### 4.1 可扩展运动基类

```csharp
public abstract class UiMotionBaseCard
{
    public abstract void Execute(Vector3 target, float speed, float delay, bool withZ);
    protected abstract void KeepMotion();
    protected abstract bool CheckFinalState();
}
```

### 4.2 三种运动类型

| 运动类型 | 类 | 插值对象 | 特殊处理 |
|----------|-----|----------|----------|
| 位置 | `UiMotionMovementCard` | `transform.position` | Z 轴锁定/解锁 |
| 旋转 | `UiMotionRotationCard` | `transform.eulerAngles` | 无 |
| 缩放 | `UiMotionScaleCard` | `transform.localScale` | 无 |

### 4.3 Z 轴控制

```csharp
// UiMotionMovementCard.cs
protected override void KeepMotion()
{
    var current = Handler.transform.position;
    var amount = Speed * Time.deltaTime;
    var delta = Vector3.Lerp(current, Target, amount);
    
    if (!WithZ)
        delta.z = Handler.transform.position.z;  // 保持 Z 不变
    
    Handler.transform.position = delta;
}
```

**用途**：
- 普通移动：保持 Z 轴不变，只在 XY 平面移动
- Draw/Discard：允许 Z 轴变化，实现 3D 动画效果

### 4.4 延迟执行

```csharp
public virtual void Execute(Vector3 vector, float speed, float delay = 0, bool withZ = false)
{
    Speed = speed;
    Target = vector;
    if (delay == 0)
        IsOperating = true;
    else
        Handler.MonoBehavior.StartCoroutine(AllowMotion(delay));
}

private IEnumerator AllowMotion(float delay)
{
    yield return new WaitForSeconds(delay);
    IsOperating = true;
}
```

**用途**：
- 抽牌时卡牌依次出现（每张延迟 0.2 秒）
- 实现错峰动画效果

## 5. 事件驱动架构亮点

### 5.1 观察者模式

```csharp
// UiCardPile.cs
public abstract class UiCardPile : MonoBehaviour, IUiCardPile
{
    private event Action<IUiCard[]> onPileChanged = hand => { };
    
    public Action<IUiCard[]> OnPileChanged
    {
        get => onPileChanged;
        set => onPileChanged = value;
    }
}
```

**事件链**：

```
┌─────────────────────────────────────────────────────────────┐
│                    事件驱动流程                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. AddCard / RemoveCard                                   │
│     │                                                       │
│     ▼                                                       │
│  2. NotifyPileChange()                                      │
│     │                                                       │
│     ▼                                                       │
│  3. onPileChanged?.Invoke(cards)                            │
│     │                                                       │
│     ├──▶ UiCardBender.Bend(cards)    → 重新排列位置         │
│     ├──▶ UiCardHandSorter.Sort(cards) → 重新设置 Z 轴      │
│     └──▶ 其他监听者...                                     │
│                                                             │
│  好处：                                                     │
│  • 添加新功能只需订阅事件                                    │
│  • 各组件独立，互不依赖                                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 输入事件封装

```csharp
// UiMouseInputProvider.cs
public class UiMouseInputProvider : MonoBehaviour, IMouseInput
{
    Action<PointerEventData> IMouseInput.OnPointerEnter { get; set; } 
        = eventData => { };
    
    Action<PointerEventData> IMouseInput.OnPointerDown { get; set; } 
        = eventData => { };
    
    // ... 其他事件
}
```

**好处**：
- 统一封装 Unity 输入接口
- 通过接口解耦，易于测试和替换

## 6. 双玩家适配亮点

### 6.1 边缘自动检测

```csharp
// TransformExtensions.cs
public static int CloserEdge(this Transform transform, Camera camera, int width, int height)
{
    var worldPointTop = camera.ScreenToWorldPoint(new Vector3(width / 2, height));
    var worldPointBot = camera.ScreenToWorldPoint(new Vector3(width / 2, 0));
    
    var deltaTop = Vector2.Distance(worldPointTop, transform.position);
    var deltaBottom = Vector2.Distance(worldPointBot, transform.position);
    
    return deltaBottom <= deltaTop ? 1 : -1;
}
```

**效果**：
- 自动判断玩家在上半屏还是下半屏
- 下方玩家：`factor = 1`，向下弯曲
- 上方玩家：`factor = -1`，向上弯曲

### 6.2 旋转方向翻转

```csharp
// UiCardBender.cs
var zAxisRot = pivotLocationFactor == 1 ? 0 : 180;
var rotation = new Vector3(0, 0, angleTwist - zAxisRot);
```

**效果**：
- 下方玩家卡牌正常朝向
- 上方玩家卡牌翻转 180°

### 6.3 速度参数区分

```csharp
// UiCardHover.cs
var speed = Handler.IsPlayer ? Parameters.RotationSpeed : Parameters.RotationSpeedP2;
```

**用途**：
- 己方玩家卡牌使用较慢的旋转速度，便于观察
- 对方玩家卡牌使用较快的旋转速度（默认 500），快速完成动画

## 7. 可扩展性亮点

### 7.1 添加新状态

只需创建新的状态类并注册：

```csharp
public class UiCardNewState : UiBaseCardState
{
    public UiCardNewState(IUiCard handler, BaseStateMachine fsm, UiCardParameters parameters) 
        : base(handler, fsm, parameters) { }
    
    public override void OnEnterState()
    {
        // 进入新状态的逻辑
    }
    
    public override void OnExitState()
    {
        // 离开新状态的逻辑
    }
}

// 在 UiCardHandFsm 中注册
public UiCardHandFsm(...) : base(handler)
{
    // ...
    NewState = new UiCardNewState(handler, this, CardConfigsParameters);
    RegisterState(NewState);
}
```

### 7.2 添加新运动类型

继承 `UiMotionBaseCard`：

```csharp
public class UiMotionCustomCard : UiMotionBaseCard
{
    protected override void KeepMotion()
    {
        // 自定义运动逻辑
    }
    
    protected override bool CheckFinalState()
    {
        // 自定义完成检测
    }
}
```

### 7.3 添加新交互

在 `IMouseInput` 接口添加事件：

```csharp
public interface IMouseInput
{
    // 现有事件
    Action<PointerEventData> OnPointerEnter { get; set; }
    
    // 新事件
    Action<PointerEventData> OnLongPress { get; set; }
}
```

## 8. 性能优化亮点

### 8.1 组件缓存

```csharp
// UiCardHandComponent.cs
private void Awake()
{
    MyTransform = transform;
    MyCollider = GetComponent<Collider>();
    MyRigidbody = GetComponent<Rigidbody>();
    MyInput = GetComponent<IMouseInput>();
    Hand = transform.parent.GetComponentInChildren<IUiCardHand>();
    MyRenderers = GetComponentsInChildren<SpriteRenderer>();
    MyRenderer = GetComponent<SpriteRenderer>();
    
    // 避免每帧 GetComponent
}
```

### 8.2 条件更新

```csharp
// UiMotionBaseCard.cs
public void Update()
{
    if (!IsOperating) return;  // 未操作时跳过
    // ...
}
```

### 8.3 事件解绑

```csharp
// 状态切换时解绑事件
public override void OnExitState()
{
    Handler.Input.OnPointerExit -= OnPointerExit;
    Handler.Input.OnPointerDown -= OnPointerDown;
}
```

**好处**：
- 避免无效的事件监听
- 减少性能开销
- 防止内存泄漏

## 9. 其他值得注意的设计

### 9.1 接口驱动

```csharp
// IUiCard 接口定义卡牌行为
public interface IUiCard
{
    void Hover();
    void Disable();
    void Enable();
    void Select();
    void Unselect();
    void Draw();
    void Discard();
    
    bool IsDragging { get; }
    bool IsHovering { get; }
    bool IsDisabled { get; }
    bool IsPlayer { get; }
}
```

**好处**：
- 面向接口编程，便于替换实现
- 易于编写单元测试
- 低耦合，高内聚

### 9.2 条件检查保护

```csharp
// 所有外部调用都有 null 检查
public void SelectCard(IUiCard card)
{
    SelectedCard = card ?? throw new ArgumentNullException("Null is not a valid argument.");
    DisableCards();
}
```

### 9.3 调试友好

```csharp
// 使用 [Button] 属性在 Inspector 中添加调试按钮
[Button]
private void NotifyCardSelected()
{
    OnCardSelected?.Invoke(SelectedCard);
}
```

**调试方法**：
- 在 Inspector 中直接调用方法
- 实时调整参数观察效果
- 便于快速验证功能

## 10. 与其他卡牌系统的对比

| 特性 | UiCard (本系统) | 传统实现 | 现代卡牌框架 |
|------|-----------------|----------|-------------|
| 状态管理 | 状态机模式 | if-else 嵌套 | 协程/异步 |
| 动画实现 | Lerp 插值 | DOTween | Timeline |
| 参数配置 | ScriptableObject | 硬编码 | ScriptableObject |
| 扩展难度 | 低 | 高 | 中 |
| 双玩家支持 | 自动检测 | 手动切换 | 自动/手动 |
| 密集选择 | 四层防护 | 单一层检测 | 多层检测 |

## 11. 使用建议

### 11.1 快速上手

1. 创建 `UiCardParameters` 配置资产
2. 配置基础参数
3. 创建卡牌预制体，添加必要组件
4. 在场景中设置手牌管理器
5. 运行测试

### 11.2 调试技巧

1. 在 `UiCardHandComponent.Update()` 中打印当前状态
2. 使用 `[Button]` 属性在 Inspector 中测试单个方法
3. 调整 `UiCardParameters` 参数实时观察效果
4. 检查 `sortingOrder` 和 Z 轴值是否正确

### 11.3 常见陷阱

1. **忘记添加 Collider**：卡牌无法响应鼠标
2. **忘记添加 Rigidbody**：物理系统报错
3. **忘记 PhysicsRaycaster**：主摄像机缺少组件
4. **参数过于极端**：导致卡牌飞出屏幕
5. **忘记禁用 Collider**：可能导致穿透问题

## 12. 总结

UiCard 系统通过精心的架构设计，为 Unity 卡牌游戏提供了：

1. ✅ **完整的交互方案**：Hover、拖拽、选择、抽牌、弃牌
2. ✅ **优雅的视觉效果**：弧形排列、平滑动画、缩放反馈
3. ✅ **灵活的配置系统**：所有参数可在 Inspector 中调整
4. ✅ **良好的可扩展性**：易于添加新状态、新运动、新交互
5. ✅ **稳定的防密集机制**：四层防护确保选择准确

无论是实现 Hearthstone 风格的竞技卡牌，还是 Slay the Spire 风格的 Roguelike 卡牌，UiCard 都能提供坚实的基础支持。