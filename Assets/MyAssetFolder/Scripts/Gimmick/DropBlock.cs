using UnityEngine;

public class DropBlock : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float touchedTime = 1f;   // これだけ乗っていれば、落下確定
    [SerializeField] private float dropTime = 2f;      // 確定後、落下するまでの時間
    [SerializeField] private float hideTime = 1.5f;    // 落下開始から非表示になるまでの時間
    [SerializeField] private float respawnTime = 3f;   // 非表示から復活するまでの時間

    [Header("Debug View")]
    [SerializeField] private float touchedTimer = 0f;
    [SerializeField] private float steppedTimer = 0f;
    [SerializeField] private float respawnTimer = 0f;
    [SerializeField] private bool isActivated = false;
    [SerializeField] private bool isDropped = false;

    private bool isTouchingPlayer = false;
    private Vector3 initialPosition;      // 初期位置の記憶

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        initialPosition = transform.position;

        ResetBlock();
    }

    private void Update()
    {
        // 1. 落下後の復活待ち処理
        if (isDropped)
        {
            respawnTimer += Time.deltaTime;

            // マジックナンバーだった 1.5f を hideTime に変更
            if (respawnTimer >= hideTime && sr.enabled)
            {
                SetBlockVisible(false);
            }

            // 指定時間（hideTime + respawnTime）が経ったら完全に復活させる
            if (respawnTimer >= hideTime + respawnTime)
            {
                ResetBlock();
            }
        }
        // 2. 乗り確定後の落下カウントダウン
        else if (isActivated)
        {
            steppedTimer += Time.deltaTime;

            if (steppedTimer >= dropTime)
            {
                Drop(); // 落下処理をメソッドに分離
            }
        }
        // 3. 乗り始めのカウントダウン
        else
        {
            if (isTouchingPlayer)
            {
                touchedTimer += Time.deltaTime;

                if (touchedTimer >= touchedTime)
                {
                    sr.color = new Color(1f, 0.5f, 0f); // オレンジ
                    isActivated = true;
                }
            }
            else
            {
                touchedTimer = Mathf.Max(0f, touchedTimer - Time.deltaTime);
            }
        }
    }

    // 落下処理
    private void Drop()
    {
        isDropped = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        //col.isTrigger = true; // 壁すり抜け
        sr.color = Color.red;
    }

    // ブロックを初期状態にリセットする関数
    private void ResetBlock()
    {
        transform.position = initialPosition;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 注: Unity 2023.2以前のバージョンをお使いの場合は、linearVelocity ではなく velocity を使用してください
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        col.isTrigger = false;
        sr.color = new Color(0.5f, 1f, 0.5f); // 緑色

        steppedTimer = 0f;
        touchedTimer = 0f;
        respawnTimer = 0f;

        isActivated = false;
        isDropped = false;
        isTouchingPlayer = false;

        SetBlockVisible(true);
    }

    private void SetBlockVisible(bool visible)
    {
        sr.enabled = visible;
        col.enabled = visible;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // プレイヤーがブロックの「上から」乗った時のみ反応させる
            // 接触面の法線ベクトルのYが下向き(-0.5以下)なら、プレイヤーは上にいる
            if (collision.contacts[0].normal.y < -0.5f)
            {
                isTouchingPlayer = true;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = false;
        }
    }
}