using UnityEngine;

// 火の矢本体。Player.ShootArrow で生成される（プレハブに付いていなければ自動で追加される）
// 松明の着火・コウモリへのダメージ・導火線の点火・綱の切断ができる（相手側が IIgnitable を実装している）
[RequireComponent(typeof(Rigidbody2D))]
public class FireArrow : MonoBehaviour
{
    [Header("寿命")]
    [SerializeField] float lifeTime = 5f;          // 何にも当たらなかったときに消えるまでの時間
    [SerializeField] float stuckLifeTime = 1f;     // 壁や地面に刺さってから消えるまでの時間

    [Header("見た目")]
    [SerializeField] bool alignToVelocity = true;  // 飛んでいる方向に矢先を向ける

    [Header("当たったら止まるもの（未設定なら Ground と Box レイヤー）")]
    [SerializeField] LayerMask stopLayers;

    Rigidbody2D rb;
    GameObject owner;
    bool isStuck;

    public bool IsStuck => isStuck;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (stopLayers.value == 0) stopLayers = LayerMask.GetMask("Ground", "Box");
        Destroy(gameObject, lifeTime);
    }

    public void SetOwner(GameObject ownerObject)
    {
        owner = ownerObject;
    }

    void FixedUpdate()
    {
        if (isStuck || !alignToVelocity) return;

        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude > 0.01f)
        {
            rb.MoveRotation(Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    void HandleHit(Collider2D other)
    {
        if (isStuck || other == null) return;
        if (IsOwner(other)) return;
        if (other.GetComponentInParent<FireArrow>() != null) return;   // 矢同士は無視

        // 火に反応するもの（松明・導火線・綱・コウモリ）
        // 親までさかのぼるのはトリガー（導火線の着火判定の子オブジェクトなど）のときだけ。
        // 振り子の箱のように「綱の子にある固い物」に当たっても綱が切れないようにする
        IIgnitable ignitable = other.GetComponent<IIgnitable>();
        if (ignitable == null && other.isTrigger)
            ignitable = other.GetComponentInParent<IIgnitable>();
        if (ignitable != null)
        {
            ignitable.Ignite(gameObject);

            // コウモリに当たった矢はそこで消える。それ以外（綱・導火線・松明）は貫通して飛び続ける
            if (ignitable is BatEnemy)
            {
                Destroy(gameObject);
            }
            return;
        }

        // 壁・地面・箱に当たったら刺さって止まる
        if (!other.isTrigger && (stopLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            Stick();
        }
    }

    bool IsOwner(Collider2D other)
    {
        if (owner == null) return other.CompareTag("Player");
        return other.transform.IsChildOf(owner.transform)
            || (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == owner);
    }

    void Stick()
    {
        isStuck = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        Destroy(gameObject, stuckLifeTime);
    }
}
