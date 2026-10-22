using UnityEngine;

// CuttableRopeと同じ切断処理だが、プレイヤーがリスポーンすると
// 綱・箱とも切る前の状態に自動的に戻る版。
// ずっと切れたままにしたい綱には CuttableRope を使ってください。
public class ResettableCuttableRope : MonoBehaviour
{
    [Header("箱を吊るしているジョイント（箱についているHingeJoint2Dなど）")]
    [SerializeField] private Joint2D boxJoint;

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

    private void OnDestroy()
    {
        Player.OnRespawn -= ResetRope;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isCut) return;

        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();

            if (player != null && (player.IsOnFire() || Input.GetKeyDown(KeyCode.Space)))
            {
                Cut();
            }
        }
    }

    private void Cut()
    {
        isCut = true;

        // 箱を吊るしているジョイントを無効化して、箱を落下させる
        if (boxJoint != null)
        {
            boxJoint.enabled = false;
        }

        // 綱の見た目・当たり判定を消す（破壊はしない＝リスポーンで戻せるように）
        gameObject.SetActive(false);
    }

    // プレイヤーがリスポーンしたときに、切る前の状態に戻す
    private void ResetRope()
    {
        if (!isCut) return;
        isCut = false;

        if (crateRigidbody != null)
        {
            crateRigidbody.linearVelocity = Vector2.zero;
            crateRigidbody.angularVelocity = 0f;
            crateRigidbody.position = crateInitialPosition;
            crateRigidbody.rotation = crateInitialRotation;
        }

        if (boxJoint != null)
        {
            boxJoint.enabled = true;
        }

        gameObject.SetActive(true);
    }
}
