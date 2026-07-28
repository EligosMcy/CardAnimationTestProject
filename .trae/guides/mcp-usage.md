# Unity MCP 使用规范

> ⚠️ **前置条件**：本节规则仅在「项目配置」中 Unity MCP 标记为「已接入」时生效。若标记为「未接入」，Agent 必须跳过本节全部规则。

## 编辑器操作优先使用 MCP 工具

- **场景搭建**（创建/摆放/删除 GameObject、设置层级关系、修改 Transform）→ 使用 `manage_gameobject`
- **资源管理**（创建/导入/移动/删除 Asset、搜索 Prefab）→ 使用 `manage_asset`
- **Prefab 操作**（创建/修改/打开 Prefab Stage）→ 使用 `manage_prefabs`
- **组件操作**（添加/移除/设置属性）→ 使用 `manage_components`
- **控制台检查**（查看编译错误、运行时日志）→ 使用 `read_console`
- **编辑器控制**（进入/退出 Play Mode、撤销/重做）→ 使用 `manage_editor`
- **场景管理**（加载/保存/创建场景、查看层级）→ 使用 `manage_scene`
- **GameObject 搜索**（按名称/标签/组件类型/路径查找）→ 使用 `find_gameobjects`

> 核心原则：**能在编辑器中完成的操作，优先用 MCP 工具而非写一次性脚本**。但运行时动态生成的对象（如战斗中按需生成的敌人实例）仍使用普通 C# 代码。

## 工具调用前确认连接状态

- 首次调用 MCP 工具前，用 `manage_editor(action="telemetry_ping")` 轻量探测 Unity Editor 是否在线
- 若连接失败（返回错误或超时）：
  - 向用户明确说明："无法连接到 Unity Editor，请确认 Unity 已打开且 MCP Bridge 已启用"
  - 等待用户确认后再继续，**禁止反复重试**
- 连接正常后，同一会话内无需重复探测

## 脚本修改后必须检查编译状态

- 完成 C# 脚本的创建/修改/删除后，必须调用 `read_console` 检查 Unity Editor 控制台
- 检查范围：**编译错误**（必须修复）、**运行时异常**（必须报告）、**警告信息**（选择性处理）
- 若 `editor_state` 资源显示 `isCompiling: true`，等待编译完成后再检查
- 发现编译错误时，立即向用户报告具体错误信息，等待确认后再修复

```text
// 标准流程
1. 修改脚本 → 2. refresh_unity（触发编译）→ 3. 等待 isCompiling 变为 false → 4. read_console 检查 → 5. 有错误则修复并回到步骤 1
```

## 参数传递规范

- 每个 MCP 工具的参数签名不同，调用前确认该工具所需的**必填参数**
- 常见必填参数：`action`（操作类型）、`target`（目标对象）、`search_method`（查找方式）
- 字符串参数直接传入字符串值，数值传入数字，布尔传入 true/false，数组/对象传入 JSON
- 不确定可选参数时先省略，用最小参数集验证调用成功后再扩展

```text
// ✅ 正确 — 按工具实际参数签名调用
manage_gameobject(action="create", primitive_type="Cube", position=[0, 0, 0])
manage_components(action="add", target="Enemy", component_type="Rigidbody")

// ❌ 错误 — 传入不存在的参数名
manage_gameobject(args="{\"action\":\"create\"}")  // 不存在 args 参数
```

## 创建 Prefab 分两步执行

- 创建 GameObject 与保存为 Prefab 是**两个独立操作**：
  1. 先用 `manage_gameobject(action="create", ...)` 在场景中创建并配置好对象
  2. 确认对象无误后，再用 `manage_prefabs(action="create_from_gameobject", target="...", prefab_path="...")` 保存
- 不要在 `manage_gameobject` 中混入 Prefab 保存参数

## 批量操作使用 batch_execute

- 当需要对多个对象执行相同操作（如创建多个 GameObject、给多个对象添加组件）时，优先使用 `batch_execute` 一次性提交
- 批量命令之间独立无依赖，失败不影响其他命令
- 单次 batch 命令数上限为 100（默认 25），超出时分批执行

## ScriptableObject 资产创建必须通过 Unity MCP

- 创建任何 `.asset` 文件前，必须先确认目标 ScriptableObject 类型是否存在于项目中
- 若类型未确认，必须先在项目中搜索或询问用户，确认后再执行创建
- 优先使用 `manage_scriptable_object(action="create")` 传入 `type_name`、`folder_path`、`asset_name` 创建资产
- 禁止直接使用文件写入工具绕过 MCP 创建 `.asset` 文件，否则会导致 GUID 不一致、元数据丢失、引用断裂等问题

### 回退机制

当 MCP 不可用或创建失败时，允许回退到直接文件写入方式，但必须在结论中说明回退原因：

| 原因分类 | 说明 |
|---------|------|
| MCP 未连接 | Unity Editor 未运行或 MCP 桥接未就绪，无法调用 `manage_scriptable_object` |
| 类型不存在 | 目标 ScriptableObject 类型在项目中未找到，MCP 无法创建该类型的资产 |
| MCP 创建失败 | `manage_scriptable_object` 调用返回错误，需附具体错误信息 |

### 创建流程

```text
1. 确认 ScriptableObject 类型（含命名空间）
2. 调用 manage_scriptable_object(action="create") 创建资产
3. 若 MCP 成功 → 完成
4. 若 MCP 失败 → 记录原因分类，回退到直接文件写入
```

## ScriptableObject 修改必须通过 Unity MCP

- 禁止直接编辑 **ScriptableObject 资产**的 `.asset` YAML 文件，必须使用 Unity MCP 工具修改
- 简单字段修改（数值、字符串、枚举、普通引用等）→ 使用 `manage_scriptable_object(action="modify")`
- `[SerializeReference]` 多态对象字段修改 → 使用 `execute_code` + `SerializedObject` API 以脚本方式写入
- **Unity 内置项目设置**（如 `TagManager.asset`、`InputManager.asset`、`EditorBuildSettings.asset` 等）不属于 ScriptableObject 资产，优先使用 Unity API 或 `execute_code` 处理，必要时可直接编辑 YAML