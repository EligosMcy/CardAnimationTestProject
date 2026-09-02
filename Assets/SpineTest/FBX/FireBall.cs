using UnityEngine;

namespace SpineTest.FBX
{
    /// <summary>
    /// 火球飞行控制：生成后沿自身前方匀速飞行，到达存活时间后自动销毁
    /// </summary>
    public class FireBall : MonoBehaviour
    {
        // ==================== 字段 ====================

        /// <summary>
        /// 飞行速度（米/秒）
        /// </summary>
        [SerializeField] private float _moveSpeed = 1f;

        /// <summary>
        /// 存活时间（秒），到时自动销毁
        /// </summary>
        [SerializeField] private float _lifeTime = 0.5f;

        // ==================== Unity 生命周期 ====================

        /// <summary>
        /// 启动时调度销毁
        /// </summary>
        private void Start()
        {
            Destroy(gameObject, _lifeTime);
        }

        /// <summary>
        /// 每帧沿自身前方匀速移动
        /// </summary>
        private void Update()
        {
            transform.Translate(transform.forward * (_moveSpeed * Time.deltaTime), Space.World);
        }
    }
}
