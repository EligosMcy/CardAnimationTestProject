# Hover 效果与防密集选择机制

## 1. 概述

本系统通过 **四层防护机制** 解决密集卡牌选择问题，确保玩家可以准确选择到任意一张卡牌。

```
┌─────────────────────────────────────────────────────────────────┐
│                    防密集选择四层防护                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Layer 1: Z 轴排序    → 防止前后遮挡                           │
│  Layer 2: 渲染层级    → Hover/Drag 卡牌置顶                    │
│  Layer 3: Collider    → 禁用非选中卡牌的碰撞                   │
│  Layer 4: 输入事件    → 只有 Idle 状态的卡牌响应               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## 2. Hover 效果详解

### 2.1 Hover 状态进入

**文件**：[UiCardHover.cs](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardStateMachine/States/UiCardHover.cs#L21-L29)

```csharp
public override void OnEnterState()
{
    MakeRenderFirst();       // Step 1: 提升渲染层级
    SubscribeInput();        // Step 2: 订阅输入事件
    CachePreviousValues();   // Step 3: 缓存当前状态
    SetScale();              // Step 4: 应用缩放
    SetPosition();           // Step 5: 应用位置偏移
    SetRotation();           // Step 6: 应用旋转
}
```

### 2.2 渲染层级提升

```csharp
// UiBaseCardState.cs#L39-L43
protected virtual void MakeRenderFirst()
{
    for (var i = 0; i < Handler.Renderers.Length; i++)
        Handler.Renderers[i].sortingOrder = LayerToRenderTop;  // = 1
}
```

**效果**：
- Hover 的卡牌 `sortingOrder` 设为 1
- 其他卡牌保持 `sortingOrder = 0`
- Hover 卡牌显示在所有其他卡牌之上

**图示**：

```
┌─────────────────────────────────────────────────────────────┐
│                    Hover 渲染层级效果                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  排序前（sortingOrder 均为 0）：                            │
│  ┌─────┐  ┌─────┐  ┌─────┐  ┌─────┐                       │
│  │  0  │  │  1  │  │  2  │  │  3  │  ← 后渲染的在上       │
│  └─────┘  └─────┘  └─────┘  └─────┘                       │
│                                                             │
│  Hover Card_2 后：                                          │
│  ┌─────┐  ┌─────┐  ┌─────┐  ┌─────┐                       │
│  │  0  │  │  1  │  │  2  │  │  3  │                       │
│  └─────┘  └─────┘  └──┬──┘  └─────┘                       │
│                        │                                    │
│              sortingOrder = 1                              │
│              (显示在最前)                                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.3 缩放效果

```csharp
// UiCardHover.cs#L92-L97
private void SetScale()
{
    var currentScale = Handler.transform.localScale;
    var finalScale = currentScale * Parameters.HoverScale;
    Handler.ScaleTo(finalScale, Parameters.ScaleSpeed);
}
```

**效果**：
- `HoverScale > 1`：卡牌放大（默认 1.3 倍）
- `HoverScale < 1`：卡牌缩小
- 通过 `UiMotionScaleCard` 平滑过渡

### 2.4 位置上移

```csharp
// UiCardHover.cs#L77-L90
private void SetPosition()
{
    var camera = Handler.MainCamera;
    
    // 半卡高度（用于计算上移量）
    var halfCardHeight = new Vector3(0, Handler.MyRenderer.bounds.size.y / 2);
    
    // 屏幕边缘世界坐标
    var bottomEdge = camera.ScreenToWorldPoint(Vector3.zero);
    var topEdge = camera.ScreenToWorldPoint(new Vector3(0, Screen.height));
    
    // 判断靠近屏幕哪个边缘
    var edgeFactor = Handler.transform.CloserEdge(camera, Screen.width, Screen.height);
    var myEdge = edgeFactor == 1 ? bottomEdge : topEdge;
    
    // 计算最终位置
    var currentPosWithoutY = new Vector3(Handler.transform.position.x, 0, Handler.transform.position.z);
    var edgeY = new Vector3(0, myEdge.y);
    var hoverHeightParameter = new Vector3(0, Parameters.HoverHeight);
    
    var final = currentPosWithoutY + edgeY + 
                (halfCardHeight + hoverHeightParameter) * edgeFactor;
    
    Handler.MoveTo(final, Parameters.HoverSpeed);
}
```

**位置计算公式**：

```
final = currentX + edgeY + (halfCardHeight + HoverHeight) × edgeFactor

其中：
• currentX: 当前 X 坐标（保持不变）
• edgeY: 屏幕边缘的 Y 坐标
• halfCardHeight: 卡牌一半高度
• HoverHeight: 参数（默认 1）
• edgeFactor: 1（靠下）或 -1（靠上）
```

**效果图示**：

```
                    屏幕顶部
┌──────────────────────────────────────────────┐
│                                              │
│    Card (Hover)                              │
│    ╱↑╲  ← HoverHeight                        │
│   ╱   ╲    上移                              │
│  ╱─────╲                                     │
│                                              │
│  ┌──────────────────────────────────────┐    │
│  │  手牌区域（正常位置）                │    │
│  │                                      │    │
│  │  ┌──┐  ┌──┐  ┌──┐  ┌──┐  ┌──┐     │    │
│  │  │  │  │  │  │  │  │  │  │  │     │    │
│  │  └──┘  └──┘  └──┘  └──┘  └──┘     │    │
│  │                                      │    │
│  └──────────────────────────────────────┘    │
│                                              │
└──────────────────────────────────────────────┘
                    屏幕底部
```

### 2.5 旋转重置

```csharp
// UiCardHover.cs#L64-L72
private void SetRotation()
{
    if (Parameters.HoverRotation)
        return;  // 保持原旋转
    
    var speed = Handler.IsPlayer ? Parameters.RotationSpeed : Parameters.RotationSpeedP2;
    Handler.RotateTo(Vector3.zero, speed);  // 旋转归零
}
```

**效果**：
- 默认 `HoverRotation = false`
- Hover 时卡牌旋转归零，正对玩家
- 设置为 `true` 可保留原始弧形角度

## 3. Hover 状态退出

### 3.1 平滑恢复

```csharp
// UiCardHover.cs#L31-L36
public override void OnExitState()
{
    ResetValues();       // 平滑恢复到原始状态
    UnsubscribeInput();  // 取消事件订阅
    DisableCollision();   // 禁用碰撞（防止误触）
}

private void ResetValues()
{
    var rotationSpeed = Handler.IsPlayer ? Parameters.RotationSpeed : Parameters.RotationSpeedP2;
    
    Handler.RotateTo(StartEuler, rotationSpeed);         // 恢复旋转
    Handler.MoveTo(StartPosition, Parameters.HoverSpeed); // 恢复位置
    Handler.ScaleTo(StartScale, Parameters.ScaleSpeed);  // 恢复缩放
}
```

**恢复流程**：

```
                    Hover → Idle 过渡
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│  Time ──────────────────────────────────────────────►       │
│                                                             │
│  Scale:  1.3x ──────► 1.0x                                 │
│          (HoverScale)   (DefaultSize)                       │
│                                                             │
│  Y Pos:  edgeY+height ──────► originalY                     │
│          (Hover位置)        (原始位置)                      │
│                                                             │
│  Rot:    0° ──────► originalAngle                           │
│          (归零)      (弧形角度)                             │
│                                                             │
│  所有变化通过 Lerp 插值，平滑过渡                           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 4. 防密集选择机制

### 4.1 问题场景

当多张卡牌密集排列时，可能出现以下问题：

```
┌─────────────────────────────────────────────────────────────┐
│                    密集卡牌问题                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  场景：5 张卡牌紧密排列                                     │
│                                                             │
│  ┌──┐  ┌──┐  ┌──┐  ┌──┐  ┌──┐                             │
│  │C0│  │C1│  │C2│  │C3│  │C4│                             │
│  └──┘  └──┘  └──┘  └──┘  └──┘                             │
│                                                             │
│  问题：                                                     │
│  • C2 被 C1 和 C3 部分遮挡                                 │
│  • 鼠标射线可能打到错误的卡牌                              │
│  • 点击 C2 实际选中 C1 或 C3                               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 Layer 1: Z 轴排序

```csharp
// UiCardHandSorter.cs#L24-L37
public void Sort(IUiCard[] cards)
{
    var layerZ = 0;
    foreach (var card in cards)
    {
        var localCardPosition = card.transform.localPosition;
        localCardPosition.z = layerZ;
        card.transform.localPosition = localCardPosition;
        layerZ += OffsetZ;  // -1
    }
}
```

**原理**：
- 每张卡有独立的 Z 值（0, -1, -2, ...）
- Z 值大的卡牌在 3D 空间中更靠近摄像机
- 鼠标射线优先命中 Z 值大的对象

**效果**：

```
┌─────────────────────────────────────────────────────────────┐
│                    Z 轴排序效果                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Z=0  ────► Card_0 (最前)                                  │
│  Z=-1 ────► Card_1                                         │
│  Z=-2 ────► Card_2                                         │
│  Z=-3 ────► Card_3                                         │
│  Z=-4 ────► Card_4 (最后)                                  │
│                                                             │
│  射线检测顺序：                                             │
│  鼠标 → Card_0 → Card_1 → ... → Card_4                    │
│                                                             │
│  最先命中 Card_0，即使视觉上被遮挡                         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 4.3 Layer 2: 渲染层级提升

```csharp
// UiBaseCardState.cs#L39-L43
protected virtual void MakeRenderFirst()
{
    for (var i = 0; i < Handler.Renderers.Length; i++)
        Handler.Renderers[i].sortingOrder = LayerToRenderTop;  // 1
}
```

**原理**：
- Hover/Drag 的卡牌 `sortingOrder = 1`
- 其他卡牌 `sortingOrder = 0`
- 高 `sortingOrder` 的 Sprite 渲染在上层

**效果**：

```
┌─────────────────────────────────────────────────────────────┐
│                    渲染层级效果                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  普通状态：                                                 │
│  ┌─────┐  ┌─────┐  ┌─────┐  ┌─────┐                       │
│  │S.O.0│  │S.O.0│  │S.O.0│  │S.O.0│  ← 全部相同           │
│  └─────┘  └─────┘  └─────┘  └─────┘                       │
│                                                             │
│  Hover Card_2 后：                                          │
│  ┌─────┐  ┌─────┐  ┌─────┐  ┌─────┐                       │
│  │S.O.0│  │S.O.0│  │S.O.1│  │S.O.0│  ← Card_2 置顶        │
│  └─────┘  └─────┘  └──┬──┘  └─────┘                       │
│                        │                                    │
│                   渲染在最前                                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 4.4 Layer 3: Collider 禁用

```csharp
// UiBaseCardState.cs#L89-L100
protected void DisableCollision()
{
    Handler.Collider.enabled = false;
}

protected void EnableCollision()
{
    Handler.Collider.enabled = true;
}
```

**时机**：
- Hover 退出时：`OnExitState()` → `DisableCollision()`
- Idle 进入时：`OnEnterState()` → `Enable()` → `EnableCollision()`

**效果**：
- 离开 Hover 后，卡牌 Collider 短暂禁用
- 防止鼠标快速移动时误触其他卡牌

### 4.5 Layer 4: 输入事件过滤

```csharp
// UiCardHover.cs#L42-L46
private void OnPointerExit(PointerEventData obj)
{
    if (Fsm.IsCurrent(this))  // 必须当前状态是 Hover
        Handler.Enable();
}

// UiCardIdle.cs#L52-L55
private void OnPointerEnter(PointerEventData obj)
{
    if (Fsm.IsCurrent(this))  // 必须当前状态是 Idle
        Handler.Hover();
}
```

**原理**：
- 每个状态只响应特定的输入事件
- 通过 `Fsm.IsCurrent(this)` 确保状态匹配
- 避免在错误状态下响应事件

**状态响应矩阵**：

| 事件 \ 状态 | Idle | Hover | Drag | Disable |
|-------------|------|-------|------|---------|
| OnPointerEnter | ✓ → Hover | ✗ | ✗ | ✗ |
| OnPointerExit | ✗ | ✓ → Idle | ✗ | ✗ |
| OnPointerDown | ✓ → Drag | ✓ → Drag | ✗ | ✗ |

## 5. 选中后的全局禁用

### 5.1 选中流程

```csharp
// UiCardHand.cs#L47-L54
public void SelectCard(IUiCard card)
{
    SelectedCard = card;
    DisableCards();      // 禁用所有其他卡牌
    NotifyCardSelected();
}

public void DisableCards()
{
    foreach (var otherCard in Cards)
        otherCard.Disable();  // → PushState<UiCardDisable>()
}
```

### 5.2 Disable 状态效果

```csharp
// UiCardDisable.cs
public class UiCardDisable : UiBaseCardState
{
    public override void OnEnterState()
    {
        Disable();
    }
}

// UiBaseCardState.cs#L73-L84
protected virtual void Disable()
{
    DisableCollision();  // 禁用射线检测
    Handler.Rigidbody.Sleep();  // 休眠刚体
    MakeRenderNormal();  // 恢复普通渲染
    foreach (var renderer in Handler.Renderers)
    {
        var myColor = renderer.color;
        myColor.a = Parameters.DisabledAlpha;  // 半透明
        renderer.color = myColor;
    }
}
```

**效果**：
- 其他卡牌变半透明（`DisabledAlpha`，默认 0.5）
- Collider 被禁用，无法被鼠标射线命中
- 视觉上提示玩家哪些卡牌不可用

**图示**：

```
┌─────────────────────────────────────────────────────────────┐
│                    选中后禁用效果                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  选中 Card_2 前：                                          │
│  ┌──┐  ┌──┐  ┌──┐  ┌──┐  ┌──┐                             │
│  │C0│  │C1│  │C2│  │C3│  │C4│  ← 全部正常               │
│  └──┘  └──┘  └──┘  └──┘  └──┘                             │
│                                                             │
│  选中 Card_2 后：                                          │
│  ┌──┐  ┌──┐  ┌──┐  ┌──┐  ┌──┐                             │
│  │C0│  │C1│  │C2│  │C3│  │C4│                             │
│  │▓▓│  │▓▓│  │██│  │▓▓│  │▓▓│  ← C2 正常, 其他半透明   │
│  │▓▓│  │▓▓│  │██│  │▓▓│  │▓▓│     Collider 全部禁用     │
│  └──┘  └──┘  └──┘  └──┘  └──┘     (除了 C2)             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 6. 完整交互时序

### 6.1 正常 Hover 流程

```
鼠标移动到 Card_2:

1. PhysicsRaycaster 检测到 Card_2
2. Card_2.OnPointerEnter 触发
3. Card_2 FSM: Idle → Hover
4. MakeRenderFirst()  → sortingOrder = 1
5. SetScale()         → 放大 1.3x
6. SetPosition()      → 上移 HoverHeight
7. SetRotation()      → 旋转归零
8. 其他卡牌不受影响（仍可正常 Hover）

鼠标移开 Card_2:

1. Card_2.OnPointerExit 触发
2. Card_2 FSM: Hover → Idle
3. ResetValues()     → 恢复原始状态
4. DisableCollision() → 临时禁用碰撞
5. 其他卡牌现在可被选中
```

### 6.2 点击选中流程

```
在 Card_2 上点击：

1. Card_2.OnPointerDown 触发（在 Hover 状态）
2. Card_2.Select() 被调用
3. Hand.SelectCard(Card_2)
   - SelectedCard = Card_2
   - DisableCards() → 所有其他卡牌进入 Disable 状态
4. Card_2 FSM: Hover → Drag
5. Card_2 跟随鼠标移动

释放鼠标：

1. Drag 结束
2. Card_2 FSM: Drag → Idle
3. 恢复弧形排列位置
4. Hand.EnableCards() → 所有卡牌恢复可用
```

## 7. 参数配置建议

### 7.1 Hover 参数

| 参数 | 建议值 | 说明 |
|------|--------|------|
| `HoverHeight` | 1.0 | 卡牌上移距离 |
| `HoverScale` | 1.2~1.4 | 卡牌放大倍数 |
| `HoverRotation` | false | 是否保留弧形旋转 |
| `HoverSpeed` | 10~20 | Hover 动画速度 |

### 7.2 防密集参数

| 参数 | 建议值 | 说明 |
|------|--------|------|
| `Spacing` | 1~3 | 卡牌间距，越大越不容易误触 |
| `DisabledAlpha` | 0.3~0.6 | 禁用时透明度 |
| `Height` | 0.1~0.2 | 弧形高度，增大可拉开卡牌 |

### 7.3 典型配置方案

**竞技游戏风格**（如 Hearthstone）：
- `HoverHeight`: 1.5
- `HoverScale`: 1.3
- `Spacing`: 2.5
- `DisabledAlpha`: 0.4

**休闲游戏风格**（如 Slay the Spire）：
- `HoverHeight`: 1.0
- `HoverScale`: 1.2
- `Spacing`: 2.0
- `DisabledAlpha`: 0.5

**极简风格**：
- `HoverHeight`: 0.8
- `HoverScale`: 1.15
- `Spacing`: 1.5
- `DisabledAlpha`: 0.6

## 8. 扩展思路

### 8.1 高亮边框

可在 Hover 状态添加额外的视觉反馈：
- 添加发光效果
- 显示卡牌描述
- 播放音效

### 8.2 锁定选择

可扩展实现卡牌锁定功能：
- 根据卡牌属性决定是否可选中
- 禁用状态的卡牌显示原因

### 8.3 多点触控

当前实现基于单鼠标，可扩展支持：
- 多点触控选择
- 双指缩放
- 手势识别