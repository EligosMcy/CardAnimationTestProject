# 卡牌交互实现机制详解

## 1. 整体架构概览

本系统基于 **状态机模式 + Unity EventSystem** 构建，分为三层架构：

```
┌─────────────────────────────────────────────────────────────────────┐
│                        卡牌交互系统架构                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │                    输入捕获层                           │       │
│   │  UiMouseInputProvider : IMouseInput                    │       │
│   │  • IPointerEnterHandler  (鼠标进入)                    │       │
│   │  • IPointerExitHandler   (鼠标离开)                    │       │
│   │  • IPointerDownHandler   (鼠标按下)                    │       │
│   │  • IPointerUpHandler     (鼠标释放)                    │       │
│   │  • IBeginDragHandler     (开始拖拽)                    │       │
│   │  • IDragHandler          (拖拽中)                      │       │
│   │  • IEndDragHandler       (结束拖拽)                    │       │
│   │  • IPointerClickHandler  (点击)                        │       │
│   └────────────────────────────┬────────────────────────────┘       │
│                                │ Action<PointerEventData>           │
│                                ▼                                     │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │                    状态机管理层                         │       │
│   │  UiCardHandFsm : BaseStateMachine                       │       │
│   │                                                         │       │
│   │    ┌──────────┐  OnPointerEnter  ┌──────────┐           │       │
│   │    │          │──────────────────▶│          │           │       │
│   │    │   Idle   │                   │  Hover   │           │       │
│   │    │          │◀──────────────────│          │           │       │
│   │    └────┬─────┘  OnPointerExit    └────┬─────┘           │       │
│   │         │                               │               │       │
│   │         │ OnPointerDown                 │ OnPointerDown │       │
│   │         ▼                               ▼               │       │
│   │    ┌──────────┐                   ┌──────────┐           │       │
│   │    │          │                   │          │           │       │
│   │    │   Drag   │                   │ Select   │           │       │
│   │    │          │                   │          │           │       │
│   │    └──────────┘                   └──────────┘           │       │
│   │                                                         │       │
│   │    ┌──────────┐                   ┌──────────┐           │       │
│   │    │          │                   │          │           │       │
│   │    │ Disable  │                   │   Draw   │           │       │
│   │    │          │                   │          │           │       │
│   │    └──────────┘                   └──────────┘           │       │
│   │                                                         │       │
│   │    ┌──────────┐                                         │       │
│   │    │          │                                         │       │
│   │    │ Discard  │                                         │       │
│   │    │          │                                         │       │
│   │    └──────────┘                                         │       │
│   └─────────────────────────────────────────────────────────┘       │
│                                │                                     │
│                                ▼                                     │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │                    运动执行层                           │       │
│   │  UiMotionBaseCard (抽象基类)                            │       │
│   │  ├── UiMotionMovementCard   (位置插值)                  │       │
│   │  ├── UiMotionRotationCard   (旋转插值)                  │       │
│   │  └── UiMotionScaleCard      (缩放插值)                  │       │
│   └─────────────────────────────────────────────────────────┘       │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## 2. 核心类关系

### 2.1 关键类职责

| 类名 | 文件路径 | 职责 |
|------|----------|------|
| `UiMouseInputProvider` | `Assets/Scripts/Tools/Input/UiMouseInputProvider.cs` | 捕获 Unity 输入事件，转换为 C# 事件 |
| `IUiCard` | `Assets/Scripts/UICard/UiCardHandComponent/IUiCard.cs` | 卡牌接口，定义所有操作 |
| `UiCardHandComponent` | `Assets/Scripts/UICard/UiCardHandComponent/UiCardHandComponent.cs` | 卡牌 MonoBehaviour，协调各系统 |
| `UiCardHandFsm` | `Assets/Scripts/UICard/UiCardStateMachine/UiCardHandFsm.cs` | 卡牌状态机 |
| `UiCardHand` | `Assets/Scripts/UICard/UiCardHand/UiCardHand.cs` | 手牌管理器 |
| `UiCardBender` | `Assets/Scripts/UICard/UiCardHand/UiCardBender.cs` | 手牌弧形排列计算 |
| `UiCardHandSorter` | `Assets/Scripts/UICard/UiCardHand/UiCardHandSorter.cs` | 卡牌 Z 轴排序 |

### 2.2 状态类

| 状态类 | 文件路径 | 触发条件 |
|--------|----------|----------|
| `UiCardIdle` | `Assets/Scripts/UICard/UiCardStateMachine/States/UiCardIdle.cs` | 初始/默认状态 |
| `UiCardHover` | `Assets/Scripts/UICard/UiCardStateMachine/States/UiCardHover.cs` | 鼠标悬停 |
| `UiCardDrag` | `Assets/Scripts/UICard/UiCardStateMachine/States/UiCardDrag.cs` | 拖拽选中 |
| `UiCardDisable` | `Assets/Scripts/UICard/UiCardStateMachine/States/UiCardDisable.cs` | 被禁用（他人选中时） |
| `UiCardDraw` | `Assets/Scripts/UICard/UiCardStateMachine/States/UiCardDraw.cs` | 抽牌动画 |
| `UiCardDiscard` | `Assets/Scripts/UICard/UiCardStateMachine/States/UiCardDiscard.cs` | 弃牌动画 |

## 3. 详细交互流程

### 3.1 输入捕获阶段

**文件**：[UiMouseInputProvider.cs](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/Tools/Input/UiMouseInputProvider.cs#L52-L175)

```csharp
public class UiMouseInputProvider : MonoBehaviour, IMouseInput
{
    // 通过 PhysicsRaycaster 检测
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        ((IMouseInput)this).OnPointerEnter.Invoke(eventData);
    }
    
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        ((IMouseInput)this).OnPointerDown.Invoke(eventData);
    }
    
    // 代理属性
    Action<PointerEventData> IMouseInput.OnPointerEnter { get; set; } 
        = eventData => { };
    
    Vector2 IMouseInput.MousePosition => Input.mousePosition;
}
```

**关键设计**：
- 使用 `PhysicsRaycaster` 而非 `GraphicRaycaster`
- 每张卡牌需要 `Collider` + `Rigidbody` 组件
- 通过 `IMouseInput` 接口解耦输入系统

### 3.2 事件订阅阶段

**文件**：[UiCardIdle.cs](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardStateMachine/States/UiCardIdle.cs#L24-L41)

```csharp
public override void OnEnterState()
{
    // 订阅输入事件
    Handler.Input.OnPointerDown += OnPointerDown;
    Handler.Input.OnPointerEnter += OnPointerEnter;
    
    if (Handler.Movement.IsOperating)
    {
        DisableCollision();  // 动画进行中，禁用碰撞
        Handler.Movement.OnFinishMotion += Enable;  // 动画完成后启用
    }
    else
    {
        Enable();  // 直接启用碰撞
    }
    
    MakeRenderNormal();  // 恢复普通渲染层级
    Handler.ScaleTo(DefaultSize, Parameters.ScaleSpeed);
}
```

### 3.3 Idle → Hover 状态转换

```csharp
// UiCardIdle.cs#L52-55
private void OnPointerEnter(PointerEventData obj)
{
    if (Fsm.IsCurrent(this))  // 确保当前仍在 Idle 状态
        Handler.Hover();
}

// UiCardHandComponent.cs#L84-L87
public void Hover()
{
    Fsm.Hover();  // → PushState<UiCardHover>()
}

// UiCardHandFsm.cs#L57-L60
public void Hover()
{
    PushState<UiCardHover>();
}
```

**Hover 状态进入**（[UiCardHover.cs](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardStateMachine/States/UiCardHover.cs#L21-L29)）：

```csharp
public override void OnEnterState()
{
    MakeRenderFirst();       // 提升渲染层级
    SubscribeInput();        // 订阅 Hover 状态的输入
    CachePreviousValues();   // 缓存当前状态
    SetScale();              // 应用缩放
    SetPosition();           // 应用位置偏移
    SetRotation();           // 应用旋转
}
```

### 3.4 Hover 状态行为

**位置计算**（[UiCardHover.cs#L77-L90](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardStateMachine/States/UiCardHover.cs#L77-L90)）：

```csharp
private void SetPosition()
{
    var camera = Handler.MainCamera;
    
    // 计算半卡高度
    var halfCardHeight = new Vector3(0, Handler.MyRenderer.bounds.size.y / 2);
    
    // 获取屏幕边缘的世界坐标
    var bottomEdge = camera.ScreenToWorldPoint(Vector3.zero);
    var topEdge = camera.ScreenToWorldPoint(new Vector3(0, Screen.height));
    
    // 判断靠近屏幕上边缘还是下边缘
    var edgeFactor = Handler.transform.CloserEdge(camera, Screen.width, Screen.height);
    var myEdge = edgeFactor == 1 ? bottomEdge : topEdge;
    
    // 计算目标位置
    var currentPosWithoutY = new Vector3(Handler.transform.position.x, 0, Handler.transform.position.z);
    var edgeY = new Vector3(0, myEdge.y);
    var hoverHeightParameter = new Vector3(0, Parameters.HoverHeight);
    var final = currentPosWithoutY + edgeY + (halfCardHeight + hoverHeightParameter) * edgeFactor;
    
    Handler.MoveTo(final, Parameters.HoverSpeed);
}
```

**缩放效果**：

```csharp
private void SetScale()
{
    var currentScale = Handler.transform.localScale;
    var finalScale = currentScale * Parameters.HoverScale;
    Handler.ScaleTo(finalScale, Parameters.ScaleSpeed);
}
```

**旋转重置**：

```csharp
private void SetRotation()
{
    if (Parameters.HoverRotation)
        return;  // 保持原旋转
    
    var speed = Handler.IsPlayer ? Parameters.RotationSpeed : Parameters.RotationSpeedP2;
    Handler.RotateTo(Vector3.zero, speed);  // 旋转归零
}
```

### 3.5 Hover → Idle 状态转换

```csharp
// UiCardHover.cs#L42-46
private void OnPointerExit(PointerEventData obj)
{
    if (Fsm.IsCurrent(this))
        Handler.Enable();  // → Fsm.Enable() → PushState<UiCardIdle>()
}

public override void OnExitState()
{
    ResetValues();     // 平滑恢复
    UnsubscribeInput();
    DisableCollision(); // 临时禁用碰撞防止误触
}

private void ResetValues()
{
    var rotationSpeed = Handler.IsPlayer ? Parameters.RotationSpeed : Parameters.RotationSpeedP2;
    Handler.RotateTo(StartEuler, rotationSpeed);        // 恢复旋转
    Handler.MoveTo(StartPosition, Parameters.HoverSpeed); // 恢复位置
    Handler.ScaleTo(StartScale, Parameters.ScaleSpeed);  // 恢复缩放
}
```

### 3.6 选择与拖拽

**点击选中**（[UiCardHandComponent.cs#L99-L107](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardHandComponent/UiCardHandComponent.cs#L99-L107)）：

```csharp
public void Select()
{
    if (!IsPlayer)  // 防止选择对方卡牌
        return;
    
    Hand.SelectCard(this);  // 通知手牌
    Fsm.Select();           // → PushState<UiCardDrag>()
}
```

**手牌选中处理**（[UiCardHand.cs#L47-L54](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardHand/UiCardHand.cs#L47-L54)）：

```csharp
public void SelectCard(IUiCard card)
{
    SelectedCard = card ?? throw new ArgumentNullException("Null is not a valid argument.");
    DisableCards();      // 禁用所有其他卡牌
    NotifyCardSelected();
}

public void DisableCards()
{
    foreach (var otherCard in Cards)
        otherCard.Disable();  // → PushState<UiCardDisable>()
}
```

**拖拽跟随**（[UiCardDrag.cs](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardStateMachine/States/UiCardDrag.cs#L40-L56)）：

```csharp
public override void OnUpdate()
{
    FollowCursor();  // 每帧跟随鼠标
}

private void FollowCursor()
{
    var myZ = Handler.transform.position.z;
    var mousePosition = Handler.Input.MousePosition;
    var worldPoint = MyCamera.ScreenToWorldPoint(mousePosition);
    Handler.transform.position = worldPoint.WithZ(myZ);
}

public override void OnEnterState()
{
    Handler.Movement.StopMotion();  // 停止之前的运动
    StartEuler = Handler.transform.eulerAngles;
    Handler.RotateTo(Vector3.zero, Parameters.RotationSpeed);  // 旋转归零
    MakeRenderFirst();  // 渲染到顶层
    RemoveAllTransparency();  // 移除透明度
}

public override void OnExitState()
{
    if (Handler.transform)
    {
        Handler.RotateTo(StartEuler, Parameters.RotationSpeed);  // 恢复旋转
        MakeRenderNormal();
    }
    DisableCollision();
}
```

## 4. 运动插值系统

### 4.1 抽象基类

**文件**：[UiMotionBaseCard.cs](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardTransform/UiMotionBaseCard.cs)

```csharp
public abstract class UiMotionBaseCard
{
    public Action OnFinishMotion = () => { };
    public bool IsOperating { get; protected set; }
    protected virtual float Threshold => 0.01f;
    protected Vector3 Target { get; set; }
    protected float Speed { get; set; }
    protected IUiCard Handler { get; }

    public void Update()
    {
        if (!IsOperating) return;
        if (CheckFinalState())
            OnMotionEnds();
        else
            KeepMotion();
    }

    public virtual void Execute(Vector3 vector, float speed, float delay = 0, bool withZ = false)
    {
        Speed = speed;
        Target = vector;
        if (delay == 0)
            IsOperating = true;
        else
            Handler.MonoBehavior.StartCoroutine(AllowMotion(delay));
    }

    public virtual void StopMotion()
    {
        IsOperating = false;
    }
}
```

### 4.2 位置插值

**文件**：[UiMotionMovementCard.cs](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardTransform/UiMotionMovementCard.cs)

```csharp
protected override void KeepMotion()
{
    var current = Handler.transform.position;
    var amount = Speed * Time.deltaTime;
    var delta = Vector3.Lerp(current, Target, amount);  // 线性插值
    if (!WithZ)
        delta.z = Handler.transform.position.z;  // 保持 Z 轴不变
    Handler.transform.position = delta;
}

protected override bool CheckFinalState()
{
    var distance = Target - Handler.transform.position;
    if (!WithZ)
        distance.z = 0;
    return distance.magnitude <= Threshold;  // 距离 < 阈值
}
```

### 4.3 旋转插值

**文件**：`Assets/Scripts/UICard/UiCardTransform/UiMotionRotationCard.cs`

```csharp
protected override void KeepMotion()
{
    var current = Handler.transform.eulerAngles;
    var amount = Speed * Time.deltaTime;
    var delta = Vector3.Lerp(current, Target, amount);
    Handler.transform.eulerAngles = delta;
}
```

### 4.4 缩放插值

**文件**：`Assets/Scripts/UICard/UiCardTransform/UiMotionScaleCard.cs`

```csharp
protected override void KeepMotion()
{
    var current = Handler.transform.localScale;
    var amount = Speed * Time.deltaTime;
    var delta = Vector3.Lerp(current, Target, amount);
    Handler.transform.localScale = delta;
}
```

## 5. 状态转换流程图

```
                              ┌───────────────────────────────────────┐
                              │                                       │
                              ▼                                       │
        ┌──────────────────────────────────────────────────────────┐   │
        │                        IDLE                              │   │
        │  • 可被鼠标射线检测                                      │   │
        │  • 显示卡牌原始形态                                      │   │
        │  • 订阅 OnPointerEnter / OnPointerDown                  │   │
        └──────────────┬───────────────────────────────────────────┘   │
                       │                                               │
        OnPointerEnter │                                               │ OnPointerDown
                       ▼                                               │
        ┌──────────────────────────────────────────────────────────┐   │
        │                        HOVER                             │   │
        │  • 渲染层级提升（sortingOrder = 1）                      │   │
        │  • 缩放到 HoverScale 倍                                  │   │
        │  • 上移 HoverHeight 单位                                 │   │
        │  • 旋转归零（可选）                                      │   │
        │  • 订阅 OnPointerExit / OnPointerDown                   │   │
        └──────────────┬───────────────────────────────────────────┘   │
                       │                                               │
        OnPointerExit  │                                               │ OnPointerDown
                       ▼                                               │
        ┌──────────────┐                              ┌──────────────┐   │
        │   回到 IDLE   │                              │    DRAG      │   │
        └──────────────┘                              │  • 跟随鼠标   │   │
                                                      │  • 旋转归零   │   │
                                                      │  • 移至顶层   │   │
                                                      │  • 解除禁用   │   │
                                                      └──────────────┘   │
                                                                       │
                                                              End Drag │
                                                                       ▼
                                                      ┌──────────────┐
                                                      │   回到 IDLE   │
                                                      │ 恢复原始状态  │
                                                      └──────────────┘
```

## 6. 关键设计模式

### 6.1 观察者模式
- `UiCardHand.OnPileChanged` 事件通知所有相关组件
- `UiMouseInputProvider` 使用 C# 事件传递输入

### 6.2 策略模式
- `UiMotionBaseCard` 抽象运动接口
- 不同运动类型实现各自的 `KeepMotion()` / `CheckFinalState()`

### 6.3 模板方法模式
- `UiBaseCardState` 提供基础实现
- 子类重写关键方法

### 6.4 状态模式
- `BaseStateMachine` 管理状态栈
- 每个状态独立封装行为逻辑

## 7. 物理组件要求

每张卡牌预制体必须包含以下组件：

| 组件 | 作用 |
|------|------|
| `BoxCollider2D` 或 `BoxCollider` | 鼠标射线检测 |
| `Rigidbody2D` 或 `Rigidbody` | 物理系统需要 |
| `UiMouseInputProvider` | 输入事件捕获 |
| `SpriteRenderer` (可多个) | 渲染卡牌各部分 |
| `UiCardHandComponent` | 卡牌主脚本 |

主摄像机需要：
- `PhysicsRaycaster` 组件（2D 或 3D）