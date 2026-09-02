using UnityEngine;
using ShowX.Utils;

public class FireBallTest : MonoBehaviour
{
    // ==================== 常量 ====================

    /// <summary>
    /// Animator Controller 中 attack 触发器的参数名
    /// </summary>
    private const string ATTACK_TRIGGER = "attack";

    // ==================== 字段 ====================

    /// <summary>
    /// 火球发射基准点（Ball Point），决定火球的生成位置与飞行方向
    /// </summary>
    [Header("火球配置")]
    [SerializeField] private Transform _ballPoint;

    /// <summary>
    /// Animator 组件缓存，Awake 中获取
    /// </summary>
    private Animator _animator;

    // ==================== Unity 生命周期 ====================

    /// <summary>
    /// 缓存 Animator 组件引用
    /// </summary>
    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 每帧监听 J 键，按下时触发攻击动画
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            triggerAttack();
        }
    }

    // ==================== 动画控制 ====================

    /// <summary>
    /// 触发 attack 触发器，由 Idle 切换到攻击动画
    /// </summary>
    private void triggerAttack()
    {
        if (_animator == null)
        {
            XLogger.LogError("FireBallTest", "triggerAttack: Animator 组件为空，请检查脚本挂载的对象");
            return;
        }

        _animator.SetTrigger(ATTACK_TRIGGER);
    }

    // ==================== 动画事件 ====================

    /// <summary>
    /// 攻击动画事件回调：在锚点位置实例化火球，并让火球沿锚点前方飞行
    /// </summary>
    /// <param name="ballObject">动画事件 Object Reference 参数中指定的火球预制体</param>
    public void AttackFireBall(GameObject ballObject)
    {
        if (ballObject == null)
        {
            XLogger.LogError("FireBallTest", "AttackFireBall: ballObject 为空，请检查攻击动画上的 AnimationEvent 对象引用参数");
            return;
        }

        if (_ballPoint == null)
        {
            XLogger.LogError("FireBallTest", "AttackFireBall: _ballPoint 未配置，请在 Inspector 的「火球配置」中指定 Ball Point");
            return;
        }

        Vector3 spawnPosition = _ballPoint.position;
        Quaternion spawnRotation = Quaternion.LookRotation(_ballPoint.forward);
        Instantiate(ballObject, spawnPosition, spawnRotation);
    }
}
