using UnityEngine;

public class PendulumRope : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField] private Transform woodBox;
    [SerializeField] private Joint2D boxJoint;
    private LineRenderer lineRenderer;

    [SerializeField] private GameObject woodBoxObject;
    public DistanceJoint2D distanceJoint2D;

    private Rigidbody2D crateRigidbody;
    private Vector2 crateInitialPosition;
    private float crateInitialRotation;
    private bool isCut = false;

    private void Awake()
    {
        if (boxJoint != null)
        {
            crateRigidbody = boxJoint.GetComponent<Rigidbody2D>();
            if (crateRigidbody != null)
            {
                crateInitialPosition = crateRigidbody.position;
                crateInitialRotation = crateRigidbody.rotation;
            }
        }

        // SetActive(false)で切られた後もリスポーンイベントを受け取れるようAwake/OnDestroyで購読する
        Player.OnRespawn += ResetRope;
    }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
        }

        if (woodBoxObject != null)
        {
            distanceJoint2D = woodBoxObject.GetComponent<DistanceJoint2D>();
        }
    }

    void LateUpdate()
    {
        // 切断されていない場合のみLineRendererを描画する
        if (!isCut && pivot != null && woodBox != null && lineRenderer != null)
        {
            lineRenderer.SetPosition(0, pivot.position);
            lineRenderer.SetPosition(1, woodBox.position);
        }
    }

    // 🌟 プレイヤーが綱の当たり判定（トリガー）に触れたときの処理
    private void OnTriggerStay2D(Collider2D collision)
    {
        // 既に切断されている場合は処理しない
        if (collision.CompareTag("Player") && !isCut)
        {
            Player player = collision.GetComponent<Player>();

            // プレイヤーが燃えている、またはスペースキーを押したら切断！
            // ※ GetKeyDown ではなく GetKey に変更しています（理由は後述）
            if (player != null && (player.IsOnFire() || Input.GetKey(KeyCode.Space)))
            {
                Cut();
            }
        }
    }

    private void OnDestroy()
    {
        Player.OnRespawn -= ResetRope;
    }

    private void Cut()
    {
        if (isCut) return; // 複数回呼ばれるのを防ぐ
        isCut = true;      // 必須: これがないと ResetRope() が機能しない

        // 1. 箱を吊るしているジョイントを無効化して、箱を落下させる
        if (boxJoint != null)
        {
            boxJoint.enabled = false;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        // 2. 綱（このオブジェクト自身）をゲーム画面から完全に消す
        // 後で ResetRope() で復活させるため、Destroy ではなく SetActive(false) を使用
        gameObject.SetActive(false);
    }

    private void ResetRope()
    {
        if (!isCut) return;
        isCut = false;

        // 箱の位置と物理演算の速度をリセット
        if (crateRigidbody != null)
        {
            crateRigidbody.linearVelocity = Vector2.zero;
            crateRigidbody.angularVelocity = 0f;
            crateRigidbody.position = crateInitialPosition;
            crateRigidbody.rotation = crateInitialRotation;
        }

        // ジョイントとLineRendererを再有効化
        if (boxJoint != null)
        {
            boxJoint.enabled = true;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }

        // 綱オブジェクトを再表示
        gameObject.SetActive(true);
    }

}