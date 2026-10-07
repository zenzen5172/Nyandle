using UnityEngine;

public class DropBlock : MonoBehaviour
{
    [SerializeField] private float dropTime = 2f;     // 着地して、落下するまでの時間
    [SerializeField] private float respawnTime = 3f;  // 落下してから復活するまでの時間
    [SerializeField] private float steppedTimer = 0f;  // 経過時間
    [SerializeField] private float touchedTime = 1f;   // これだけ乗っていれば、落下確定
    [SerializeField] private float touchedTimer = 0f;
    [SerializeField] private bool isActivated = false;
    [SerializeField] private bool isDropped = false;   // 落下処理が実行されたかどうかのフラグ

    private bool isTouchingPlayer = false;
    private Vector3 initialPosition;      // 初期位置の記憶
    private float respawnTimer = 0f;      // 復活用のタイマー

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        // ゲーム開始時の位置を記憶
        initialPosition = transform.position;

        // 初期状態にセット
        ResetBlock();
    }

    private void Update()
    {
        // 1. 落下後の復活待ち処理
        if (isDropped)
        {
            respawnTimer += Time.deltaTime;

            // 落下から1.5秒経過したら、姿と当たり判定を消して初期位置へ移動
            if (respawnTimer >= 1.5f && sr.enabled)
            {
                SetBlockVisible(false);
            }

            // 指定時間（1.5秒 + respawnTime秒）が経ったら完全に復活させる
            if (respawnTimer >= 1.5f + respawnTime)
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
                isDropped = true;
                rb.bodyType = RigidbodyType2D.Dynamic;
                col.isTrigger = true; // 壁すり抜け
                sr.color = Color.red;
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
                    sr.color = new Color(1f, 0.5f, 0f);
                    isActivated = true;
                }
            }
            else
            {
                touchedTimer = Mathf.Max(0f, touchedTimer - Time.deltaTime);
            }
        }
    }

    // ブロックを初期状態にリセットする関数
    private void ResetBlock()
    {
        // 位置と物理挙動のリセット
        transform.position = initialPosition;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero; // 移動速度をクリア
        rb.angularVelocity = 0f;

        // コライダーと色のリセット
        col.isTrigger = false;
        sr.color = new Color(0.5f, 1f, 0.5f);

        // タイマーとフラグのリセット
        steppedTimer = 0f;
        touchedTimer = 0f;
        respawnTimer = 0f;

        isActivated = false;
        isDropped = false;
        isTouchingPlayer = false;

        // 見た目と当たり判定を表示
        SetBlockVisible(true);
    }

    // 表示・非表示を切り替える関数
    private void SetBlockVisible(bool visible)
    {
        sr.enabled = visible;
        col.enabled = visible;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = true;
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