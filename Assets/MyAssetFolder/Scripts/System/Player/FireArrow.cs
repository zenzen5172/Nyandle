using System.Collections.Generic;
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

    [Header("当たったら止まるもの（未設定なら Player レイヤー以外の固い物すべて）")]
    [SerializeField] LayerMask stopLayers;

    Rigidbody2D rb;
    GameObject owner;
    bool isStuck;
    bool isFinished;                // 刺さった・消えることが決まった
    Vector2 lastPosition;
    readonly HashSet<Collider2D> handledColliders = new HashSet<Collider2D>();
    readonly List<RaycastHit2D> sweepHits = new List<RaycastHit2D>();

    public bool IsStuck => isStuck;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 地面(Ground)・箱(Box)だけでなく、鍵付きドアや天井ブロック(Default)でも止まるようにする
        if (stopLayers.value == 0) stopLayers = ~LayerMask.GetMask("Player");
        lastPosition = rb.position;
        Destroy(gameObject, lifeTime);
    }

    public void SetOwner(GameObject ownerObject)
    {
        if (owner == null) Player.OnRespawn += RemoveOnRespawn;
        owner = ownerObject;
    }

    void OnDestroy() => Player.OnRespawn -= RemoveOnRespawn;

    void RemoveOnRespawn()
    {
        isFinished = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (isFinished) return;

        // 矢は速いので、細い綱（幅0.1）などを1フレームで飛び越えてしまうことがある。
        // 前回の位置から今の位置までを線で調べて、通り過ぎたものにも当てる
        SweepFrom(lastPosition, rb.position);
        lastPosition = rb.position;
        if (isFinished) return;

        if (alignToVelocity)
        {
            Vector2 v = rb.linearVelocity;
            if (v.sqrMagnitude > 0.01f)
            {
                rb.MoveRotation(AngleForDirection(v));
            }
        }
    }

    void SweepFrom(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.0001f) return;

        var filter = ContactFilter2D.noFilter;
        filter.useTriggers = true;
        sweepHits.Clear();
        Physics2D.Raycast(from, delta / distance, filter, sweepHits, distance);
        sweepHits.Sort((a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in sweepHits)
        {
            HandleHit(hit.collider, hit.point);
            if (isFinished) break;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other, null);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider, null);
    }

    // hitPoint: 線で調べて見つけたときの当たった位置（刺さる位置に使う）
    void HandleHit(Collider2D other, Vector2? hitPoint)
    {
        if (isFinished || other == null) return;
        if (!handledColliders.Add(other)) return;                    // 同じ相手には1回だけ反応する
        if (IsOwner(other)) return;
        if (other.GetComponentInParent<FireArrow>() != null) return;   // 矢同士は無視

        if (owner != null && owner.TryGetComponent(out Player player) && player.CanBurnWoodBoxes
            && other.TryGetComponent(out BurnableWoodBox woodBox))
        {
            woodBox.Ignite();
        }

        // 火に反応するもの（松明・導火線・綱・コウモリ）
        // 親までさかのぼるのはトリガー（導火線の着火判定の子オブジェクトなど）のときだけ。
        // 振り子の箱のように「綱の子にある固い物」に当たっても綱が切れないようにする
        IIgnitable ignitable = other.GetComponent<IIgnitable>();
        if (ignitable == null && other.isTrigger)
            ignitable = other.GetComponentInParent<IIgnitable>();
        if (ignitable != null)
        {
            ignitable.Ignite(gameObject);

            // コウモリは矢を消費し、綱・導火線などは貫通する。
            if (ignitable is BatEnemy)
            {
                isFinished = true;
                Destroy(gameObject);
            }
            return;
        }

        // 壁・地面・箱・ドアに当たったら刺さって止まる（下から通り抜けられる足場はすり抜ける）
        if (!other.isTrigger && !IsOneWayPlatform(other)
            && (stopLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            Stick(hitPoint);
        }
    }

    // 矢のスプライトとコライダーの先端はローカル座標の +Y を向く。
    internal static float AngleForDirection(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

    static bool IsOneWayPlatform(Collider2D other)
    {
        if (!other.usedByEffector) return false;
        var effector = other.GetComponent<PlatformEffector2D>();
        return effector != null && effector.useOneWay;
    }

    bool IsOwner(Collider2D other)
    {
        if (owner == null) return other.CompareTag("Player");
        return other.transform.IsChildOf(owner.transform)
            || (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == owner);
    }

    void Stick(Vector2? hitPoint)
    {
        isStuck = true;
        isFinished = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (hitPoint.HasValue) rb.position = hitPoint.Value;   // 通り過ぎていたら当たった位置に戻す
        Destroy(gameObject, stuckLifeTime);
    }
}
