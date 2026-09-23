using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("移動")]
    public float speed = 5f;
    public float gravityScale = 2.5f;   // 重力

    [Header("接地判定")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.08f;   // 足元をどれだけの厚みで調べるか
    public float wallCheckDistance = 0.04f;     // 進行方向の壁をどれだけ手前で調べるか

    [Header("ジャンプ(タイル基準)")]
    public float tileSize = 1f;           // Tilemapの1マスの大きさ(Grid の Cell Size に合わせる)
    public int minJumpBlocks = 3;         // タップで乗り越えられる壁の高さ(マス数)
    public int maxJumpBlocks = 5;         // 長押しで乗り越えられる壁の高さ(マス数)
    public float jumpClearance = 0.25f;   // 壁の上に乗れるよう、指定マス数より余分に飛ぶ高さ(マス数)
    public float holdStartDelay = 0.08f;  // これ以下の短いタップはminちょうどになる
    public float holdTimeToMax = 0.25f;   // これだけ押し続けるとmaxに届く。途中で離せばその分の高さ
    public float coyoteTime = 0.1f;       // 崖を踏み外した後もジャンプを受け付ける猶予
    public float jumpBufferTime = 0.1f;   // 着地直前の入力を覚えておく時間

    [Header("見た目")]
    public Sprite idleSprite;
    public Sprite rightSprite;
    public Sprite leftSprite;
    public Sprite jumpSprite;

    [Header("頭上の炎(S / 下キー)")]
    public Vector2 flameSize = new Vector2(1f, 1f);   // 当たり判定の大きさ
    public float flameHeightOffset = 1f;              // プレイヤー中心からどれだけ上に出すか
    public Sprite flameSprite;                        // 未設定なら判定だけ出る(見た目なし)

    [Header("音声")]
    public AudioClip jumpSound;

    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer spriteRenderer;
    AudioSource audioSource;
    GameObject flame;

    float jumpBuffer;
    float coyoteTimer;
    bool isGrounded;

    bool isRising;    // 自分のジャンプで上昇中か
    bool canBoost;    // まだ長押しでmaxまで伸ばせるか
    float jumpTime;
    float jumpStartY;

    int maxJumpCount = 1;     // 普段は1(地上ジャンプのみ)。イベントで増やすと空中ジャンプが可能になる
    int jumpCount;
    bool groundJumpSpent;     // この滞空中に地上ジャンプ分を使った or 失ったか

    float lastX;

    public bool IsGrounded => isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        rb.freezeRotation = true;   // コライダーが地面との摩擦で転がるのを防ぐ
        rb.gravityScale = gravityScale;
        jumpCount = maxJumpCount;
        CreateFlame();
    }

    void CreateFlame()
    {
        flame = new GameObject("Flame");
        flame.transform.SetParent(transform);
        flame.transform.localPosition = new Vector3(0f, flameHeightOffset, 0f);
        flame.transform.localScale = Vector3.one;

        var box = flame.AddComponent<BoxCollider2D>();
        box.size = flameSize;
        box.isTrigger = true;

        if (flameSprite != null)
        {
            // 判定用オブジェクトを拡大するとコライダーまで歪むので、見た目は別の子に分ける
            var visual = new GameObject("Visual");
            visual.transform.SetParent(flame.transform);
            visual.transform.localPosition = Vector3.zero;

            Vector3 spriteSize = flameSprite.bounds.size;
            visual.transform.localScale = new Vector3(
                spriteSize.x > 0f ? flameSize.x / spriteSize.x : 1f,
                spriteSize.y > 0f ? flameSize.y / spriteSize.y : 1f,
                1f);

            visual.AddComponent<SpriteRenderer>().sprite = flameSprite;
        }

        flame.SetActive(false);
    }

    // 今の滞空中だけ有効なジャンプを与える。着地すると残り回数がリセットされて消える
    public void GrantExtraJump(int amount = 1)
    {
        jumpCount += amount;
    }

    float Gravity => Mathf.Abs(Physics2D.gravity.y) * gravityScale;

    // 高さ height にちょうど届く初速。物理エンジンの離散積分で頂点が v*dt/2 だけ低くなる分も補正する
    float PowerForHeight(float height)
    {
        float g = Gravity;
        float gdt = g * Time.fixedDeltaTime;
        return (gdt + Mathf.Sqrt(gdt * gdt + 8f * g * height)) * 0.5f;
    }

    // そのマス数の壁の上に乗れるように、少しだけ余分に飛ぶ
    float HeightForBlocks(float blocks) => (blocks + jumpClearance) * tileSize;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame)
            jumpBuffer = jumpBufferTime;

        // S / 下キーを押している間だけ頭上に炎の判定を出す
        bool flameHeld = kb.sKey.isPressed || kb.downArrowKey.isPressed;
        if (flame.activeSelf != flameHeld)
            flame.SetActive(flameHeld);
    }

    void FixedUpdate()
    {
        jumpBuffer -= Time.fixedDeltaTime;

        isGrounded = CheckGrounded();

        if (isGrounded && !isRising)
        {
            coyoteTimer = coyoteTime;
            jumpCount = maxJumpCount;   // アイテムで一時的に増えた分もここで消える
            groundJumpSpent = false;
        }
        else
        {
            coyoteTimer -= Time.fixedDeltaTime;
            // ジャンプせずに落ちた場合、猶予が切れた時点で地上ジャンプ分を失う
            if (coyoteTimer <= 0f && !groundJumpSpent)
            {
                groundJumpSpent = true;
                jumpCount--;
            }
        }

        HorizontalMove();
        Jump();
        KillUnwantedLift();
        UpdateAppearance();
    }

    // 足元の薄い帯だけを調べる。横幅を本体より狭めることで、横の壁を地面と誤検知しない
    bool CheckGrounded()
    {
        Bounds b = col.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckDistance * 0.5f);
        Vector2 size = new Vector2(b.size.x * 0.9f, groundCheckDistance);
        return Physics2D.OverlapBox(center, size, 0f, groundLayer);
    }

    // 進行方向のすぐ横を調べる。足元と頭は少し削って、床や天井を壁と誤検知しないようにする
    bool IsTouchingWall(float dir)
    {
        Bounds b = col.bounds;
        float height = b.size.y - groundCheckDistance * 2f;
        if (height <= 0f) return false;

        float edgeX = dir > 0f ? b.max.x : b.min.x;
        Vector2 center = new Vector2(edgeX + dir * wallCheckDistance * 0.5f, b.center.y);
        Vector2 size = new Vector2(wallCheckDistance, height);
        return Physics2D.OverlapBox(center, size, 0f, groundLayer);
    }

    void HorizontalMove()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float x = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x = -1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x = 1f;

        lastX = x;

        // 壁に向かって速度を与え続けると、角にめり込んで斜め上に押し出され、よじ登ってしまう。
        // 壁がある方向へはそもそも押し込まない
        if (x != 0f && IsTouchingWall(x))
            x = 0f;

        rb.linearVelocity = new Vector2(x * speed, rb.linearVelocity.y);
    }

    void Jump()
    {
        var kb = Keyboard.current;
        bool held = kb != null && kb.spaceKey.isPressed;

        if (isRising)
        {
            jumpTime += Time.fixedDeltaTime;

            if (rb.linearVelocity.y <= 0f)
            {
                isRising = false;   // 頂点に到達
                canBoost = false;
            }
            else if (!held)
            {
                canBoost = false;   // 離した時点の高さで確定。あとは重力に任せる
            }
            else if (canBoost && jumpTime <= holdTimeToMax)
            {
                // 押している長さに応じて目標の高さを伸ばし、そこがちょうど頂点になる速度に調整し続ける。
                // 実際に登った分を引いて計算するので、いつ離してもその時点の目標高さが正確に頂点になる
                float t = Mathf.InverseLerp(holdStartDelay, holdTimeToMax, jumpTime);
                float targetHeight = HeightForBlocks(Mathf.Lerp(minJumpBlocks, maxJumpBlocks, t));
                float remaining = targetHeight - (rb.position.y - jumpStartY);
                if (remaining > 0f)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, PowerForHeight(remaining));
            }
        }

        if (jumpBuffer > 0f && (coyoteTimer > 0f || jumpCount > 0))
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x, PowerForHeight(HeightForBlocks(minJumpBlocks)));
            jumpStartY = rb.position.y;
            jumpTime = 0f;
            isRising = true;
            canBoost = true;
            jumpBuffer = 0f;
            coyoteTimer = 0f;
            jumpCount--;
            groundJumpSpent = true;

            if (jumpSound != null)
                audioSource.PlayOneShot(jumpSound);
        }
    }

    // 自分のジャンプ以外で上向きの速度が付いたら打ち消す(壁や角での跳ね上げ防止)
    void KillUnwantedLift()
    {
        if (!isRising && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
    }

    void UpdateAppearance()
    {
        Sprite target;
        if (!isGrounded)
            target = jumpSprite;
        else if (lastX > 0f)
            target = rightSprite;
        else if (lastX < 0f)
            target = leftSprite;
        else
            target = idleSprite;

        // 未設定の状態は今の見た目を維持する(消えたりしないように)
        if (target != null)
            spriteRenderer.sprite = target;
    }
}
