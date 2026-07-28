# Unity 专项规则

## SerializeField 私有字段保持 `_` 前缀

```csharp
[SerializeField] private GameObject _enemyPrefab;
[SerializeField] private float _animationDuration = 0.3f;
```

## Inspector 分组使用 `[Header]`

```csharp
[Header("敌人配置")]
[SerializeField] private EnemyData _enemyData;

[Header("动画参数")]
[SerializeField] private float _tweenDuration = 0.3f;
```

## Awake 初始化自身，Start 获取外部引用

```csharp
private void Awake()
{
    // 初始化自己的组件/字段
    _rigidbody = GetComponent<Rigidbody2D>();
}

private void Start()
{
    // 获取其他对象的引用
    _gameFlowSystem = GameFlowSystem.Instance;
}
```

## 缓存 Camera.main，禁止在 Update 中调用

```csharp
// ❌ 错误
void Update() { Vector3 pos = Camera.main.WorldToScreenPoint(transform.position); }

// ✅ 正确
private Camera _mainCamera;
private void Start() { _mainCamera = Camera.main; }
void Update() { Vector3 pos = _mainCamera.WorldToScreenPoint(transform.position); }
```

## 使用 CompareTag() 而非 `.tag ==`

```csharp
// ❌ 错误 — 会产生 GC 分配
if (other.gameObject.tag == "Player") { }

// ✅ 正确
if (other.gameObject.CompareTag("Player")) { }
```

## 使用 nameof() 而非字符串硬编码

```csharp
// ❌ 错误 — 重构不安全
Invoke("DoSomething", 1f);
SendMessage("OnDamage", damage);

// ✅ 正确
Invoke(nameof(DoSomething), 1f);
SendMessage(nameof(OnDamage), damage);
```

## 协程中缓存 WaitForSeconds

```csharp
// ❌ 错误 — 每次 yield 都 new，产生 GC
while (true) { yield return new WaitForSeconds(0.5f); }

// ✅ 正确
private readonly WaitForSeconds _waitHalfSecond = new WaitForSeconds(0.5f);
while (true) { yield return _waitHalfSecond; }
```

## 优先使用 TryGetComponent

```csharp
// ✅ 推荐 — 性能更好
if (other.TryGetComponent<EnemyView>(out EnemyView enemyView))
{
    enemyView.TakeDamage(damage);
}
```

## 禁止在 Update/FixedUpdate 中调用查找方法

- `FindObjectOfType`、`GameObject.Find`、`GetComponent` 等高开销查找必须在 `Awake()` 或 `Start()` 中完成并缓存
- Update 中只能使用已缓存的引用