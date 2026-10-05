using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
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
    public float maxJumpBlocks = 3.5f;    // 長押しで乗り越えられる壁の高さ(マス数)
    public float jumpClearance = 0.25f;   // 壁の上に乗れるよう、指定マス数より余分に飛ぶ高さ(マス数)
    public float holdStartDelay = 0.08f;  // これ以下の短いタップはminちょうどになる
    public float holdTimeToMax = 0.25f;   // これだけ押し続けるとmaxに届く。途中で離せばその分の高さ
    public float coyoteTime = 0.1f;       // 崖を踏み外した後もジャンプを受け付ける猶予
    public float jumpBufferTime = 0.1f;   // 着地直前の入力を覚えておく時間

    [Header("大砲に関する行動制限")]
    public bool canControl = true;
    public bool isLaunched = false;

    [Header("発光・火の判定")]
    public Color glowColor = Color.yellow;
    public float fireDuration = 0.1f;
    public bool jumpable = false;
    private float fireTimer = 0f;

    [Header("見た目")]
    Animator animator;

    [Header("頭上の炎(S / 下キー)")]
    public Vector2 flameSize = new Vector2(1f, 1f);   // 当たり判定の大きさ
    public float flameHeightOffset = 1f;              // プレイヤー中心からどれだけ上に出すか
    public Sprite flameSprite;                        // 未設定なら判定だけ出る(見た目なし)

    [Header("死亡・リスポーン")]
    public Vector2 respawnPoint;         // CheckPointLantern などから書き換えられる。初期値は開始位置
    public float respawnDelay = 0.5f;    // 死んでから復活するまでの時間

    [Header("音声")]
    public AudioClip jumpSound;
    public AudioClip deathSound;
    public AudioClip flameSound;   // 炎を出している間ループ再生する

    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer spriteRenderer;
    AudioSource audioSource;
    GameObject flame;

    float jumpBuffer;
    float coyoteTimer;
    bool isGrounded;
    public bool isGround => isGrounded;

    bool isRising;    // 自分のジャンプで上昇中か
    bool canBoost;    // まだ長押しでmaxまで伸ばせるか
    float jumpTime;
    float jumpStartY;

    [Header("ジャンプ回数")]
    [SerializeField] int maxJumpCount = 0;     // 普段は0(空中ジャンプ不可。ReJumpStone取得時のみ追加)
    [SerializeField] int jumpCount;
    [SerializeField] bool groundJumpSpent;     // この滞空中に地上ジャンプ分を使った or 失ったか

    float lastX;
    bool isDead;

    public bool IsGrounded => isGrounded;
    public bool IsDead => isDead;

    // 🌟【追加】CannonFuseやCuttableRopeが参照する「燃えている状態か」の判定
    public bool IsOnFire()
    {
        // 頭上に炎を出している(S/下キー) か、ジャンプ等で発光・点火中であれば true
        return (flame != null && flame.activeSelf) || fireTimer > 0f;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
        animator = gameObject.GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        rb.freezeRotation = true;   // コライダーが地面との摩擦で転がるのを防ぐ
        rb.gravityScale = gravityScale;
        jumpCount = maxJumpCount;
        respawnPoint = transform.position;
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

        if (flameSound != null)
        {
            var flameAudio = flame.AddComponent<AudioSource>();
            flameAudio.clip = flameSound;
            flameAudio.loop = true;
            flameAudio.playOnAwake = true;
        }

        flame.SetActive(false);
    }

    // 死亡処理（BatEnemy等から攻撃を受けた際など）
    public void Die()
    {
        if (isDead) return;

        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;   // 死亡中に物理挙動を停止
        spriteRenderer.enabled = false;
        flame.SetActive(false);

        if (deathSound != null)
            audioSource.PlayOneShot(deathSound);

        Invoke(nameof(Respawn), respawnDelay);
    }

    // リスポーン処理（BatEnemyやCannonの失敗・リセット時にも呼ばれる）
    public void Respawn()
    {
        CancelInvoke(nameof(Respawn));

        transform.position = new Vector3(respawnPoint.x, respawnPoint.y, transform.position.z);
        rb.simulated = true;
        rb.position = respawnPoint;
        rb.linearVelocity = Vector2.zero;

        jumpBuffer = 0f;
        coyoteTimer = 0f;
        isRising = false;
        canBoost = false;
        jumpCount = maxJumpCount;
        groundJumpSpent = false;

        jumpable = false;
        canControl = true;
        isLaunched = false;

        spriteRenderer.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        isDead = false;
    }

    public void GrantExtraJump(int amount = 1)
    {
        jumpCount += amount;
    }

    float Gravity => Mathf.Abs(Physics2D.gravity.y) * gravityScale;

    float PowerForHeight(float height)
    {
        float g = Gravity;
        float gdt = g * Time.fixedDeltaTime;
        return (gdt + Mathf.Sqrt(gdt * gdt + 8f * g * height)) * 0.5f;
    }

    float HeightForBlocks(float blocks) => (blocks + jumpClearance) * tileSize;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || isDead) return;

        // canControlがtrueの時、または空中・ジャンプ可能な時に入力を受付
        if (canControl || isGrounded || jumpable || jumpCount > 0)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                jumpBuffer = jumpBufferTime;
        }

        // 点火タイマーとカラー更新
        if (fireTimer > 0)
        {
            fireTimer -= Time.deltaTime;
            spriteRenderer.color = Color.orange;
        }
        else
        {
            spriteRenderer.color = (jumpable || isGrounded) ? glowColor : Color.gray;
        }

        // S / 下キーを押している間、頭上に炎を出して当たり判定を有効化
        bool flameHeld = kb.sKey.isPressed || kb.downArrowKey.isPressed;
        if (flame.activeSelf != flameHeld)
            flame.SetActive(flameHeld);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        jumpBuffer -= Time.fixedDeltaTime;

        isGrounded = CheckGrounded();
        if (isGrounded)
        {
            isLaunched = false; // 地面に着地したら大砲発射フラグを解除
        }

        if (isGrounded && !isRising)
        {
            coyoteTimer = coyoteTime;
            jumpCount = maxJumpCount;
            groundJumpSpent = false;
        }
        else
        {
            coyoteTimer -= Time.fixedDeltaTime;
            if (coyoteTimer <= 0f && !groundJumpSpent)
            {
                groundJumpSpent = true;
                jumpCount--;
            }
        }

        // 大砲の中に入っている時や硬直中(canControl == false)は移動入力をパスする
        if (canControl)
        {
            HorizontalMove();
        }

        Jump();
        KillUnwantedLift();
        UpdateAppearance();
    }

    bool CheckGrounded()
    {
        if (col == null) return false;
        Bounds b = col.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckDistance * 0.5f);
        Vector2 size = new Vector2(b.size.x * 0.95f, groundCheckDistance);
        return Physics2D.OverlapBox(center, size, 0f, groundLayer);
    }

    // 🌟一方通行（Effector）が設定されている壁をめり込み防止処理から除外するよう修正
    bool IsTouchingWall(float dir)
    {
        if (col == null) return false;
        Bounds b = col.bounds;
        float height = b.size.y - groundCheckDistance * 2f;
        if (height <= 0f) return false;

        float edgeX = dir > 0f ? b.max.x : b.min.x;
        Vector2 center = new Vector2(edgeX + dir * wallCheckDistance * 0.5f, b.center.y);
        Vector2 size = new Vector2(wallCheckDistance, height);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, groundLayer);
        foreach (var hit in hits)
        {
            // 自分自身以外のコライダーで、かつ Effector（一方通行）が使われていない通常の壁の場合のみブロック
            if (hit != col && !hit.usedByEffector)
            {
                return true;
            }
        }

        return false;
    }

    void HorizontalMove()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float x = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x = -1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x = 1f;

        lastX = x;

        if (x != 0f && IsTouchingWall(x))
            x = 0f;

        rb.linearVelocity = new Vector2(x * speed, rb.linearVelocity.y);
    }

    void Jump()
    {
        var kb = Keyboard.current;
        bool held = kb != null && (kb.upArrowKey.isPressed || kb.wKey.isPressed || kb.spaceKey.isPressed);

        if (isRising)
        {
            jumpTime += Time.fixedDeltaTime;

            if (rb.linearVelocity.y <= 0f)
            {
                isRising = false;
                canBoost = false;
            }
            else if (!held)
            {
                canBoost = false;
            }
            else if (canBoost && jumpTime <= holdTimeToMax)
            {
                float t = Mathf.InverseLerp(holdStartDelay, holdTimeToMax, jumpTime);
                float targetHeight = HeightForBlocks(Mathf.Lerp(minJumpBlocks, maxJumpBlocks, t));
                float remaining = targetHeight - (rb.position.y - jumpStartY);
                if (remaining > 0f)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, PowerForHeight(remaining));
            }
        }

        if (jumpBuffer > 0f && (isGrounded || coyoteTimer > 0f || jumpCount > 0 || jumpable))
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x, PowerForHeight(HeightForBlocks(minJumpBlocks)));
            jumpStartY = rb.position.y;
            jumpTime = 0f;
            isRising = true;
            canBoost = true;
            jumpBuffer = 0f;
            coyoteTimer = 0f;

            canControl = true;
            isLaunched = false;
            jumpable = false;

            if (!isGrounded && coyoteTimer <= 0f && jumpCount > 0)
            {
                jumpCount--;
            }
            groundJumpSpent = true;

            fireTimer = fireDuration;

            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }

            if (jumpSound != null)
                audioSource.PlayOneShot(jumpSound);
        }
    }

    // 大砲で吹き飛ばされている最中(isLaunched == true)以外の上向きの異常な跳ね上がりを防止
    void KillUnwantedLift()
    {
        if (isLaunched) return;

        if (!isRising && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
    }

    void UpdateAppearance()
    {
        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            animator.SetFloat("vSpeed", rb.linearVelocity.y);
        }

        if (lastX > 0f)
        {
            spriteRenderer.flipX = false;
        }
        else if (lastX < 0f)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (col == null) return;

        Bounds b = col.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckDistance * 0.5f);
        Vector2 size = new Vector2(b.size.x * 0.95f, groundCheckDistance);

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(center, size);
    }
}