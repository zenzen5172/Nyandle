using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    //リスポーン時に呼ばれるイベント（他のオブジェクトはこれを購読して状態をリセットできる）
    public static event Action OnRespawn;

    //動く速さ
    [SerializeField] float moveSpeed = 5f;

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
    [SerializeField] public float gravityScale = 1.5f;

    [Header("大砲に関する行動制限")]
    public bool canControl = true;
    public bool isLaunched = false;
    public float launchAirControl = 20f;   // 大砲で飛ばされている間、左右入力で速度を変えられる加速度（小さいほど慣性が残る）
    public bool addJumpToLaunch = false;   // true: 発射の上向きの勢いにジャンプ力を足す / false: 勢いとジャンプ力の大きい方を使う

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

    [Header("リスポーン直後の操作不能時間（秒）")]
    public float respawnControlLockTime = 0.3f;
    float controlLockTimer;
    bool IsInputLocked => controlLockTimer > 0f;

    [Header("音声")]
    public AudioClip jumpSound;
    public AudioClip deathSound;
    public AudioClip flameSound;   // 炎を出している間ループ再生する
    public AudioClip arrowShotSound; // 矢を撃ったときの音（未設定なら鳴らない）
    bool jumpSoundPlayedEarly;     // FixedUpdateを待たずにUpdateで先にジャンプ音を鳴らしたか

    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer spriteRenderer;
    AudioSource audioSource;
    GameObject flame;
    SpriteRenderer sr;

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


    // 🌟【追加】弓矢に関する設定
    [Header("弓矢(Mキー)")]
    public GameObject bowAndArrow;      // 構えている用の弓矢オブジェクト(Playerの子オブジェクト)
    public GameObject arrowPrefab;      // 発射する矢のプレハブ
    public Transform arrowSpawnPoint;   // 矢が発射される位置と向き（構え中は毎フレーム弓の先に合わせて動かす）
    public float swingSpeed = 5f;       // 振り子の往復スピード
    public float swingAngle = 60f;      // 左右(上下)の最大角度 (60度なら合計120度)
    public float shootForce = 20f;      // 矢を発射する力
    private bool isAiming = false;      // 弓を構えているか
    public float bowTimeout = 5f;       // 【追加】弓が強制的に消えるまでの時間（秒）
    private float aimTimer = 0f;
    public float intervalArrow = 1f;    // 1回撃ってから次に撃てるまでの時間（秒）
    public float arrowSpawnDistance = 0.3f; // 弓の位置から、矢を出す位置までの距離

    [Header("弓矢の向きガイド（薄い矢印）")]
    public bool showAimGuide = true;
    public float aimGuideLength = 2f;
    public float aimGuideWidth = 0.05f;
    public Color aimGuideColor = new Color(1f, 0.6f, 0.2f, 0.35f);
    public Material aimGuideMaterial;   // 未設定なら Sprites/Default を使う

    float intervalArrowTimer;           // 次に撃てるまでの残り時間（0なら撃てる）
    float currentAimAngle;              // 水平からの角度（上がプラス）
    Vector2 bowBaseLocalPosition;       // 右向き時の弓のローカル位置
    Vector3 bowBaseLocalScale;
    LineRenderer aimGuide;

    // 火の弓矢UI用（awaizaさんのUIから参照）
    public float IntervalArrowRemaining => intervalArrowTimer;
    public float IntervalArrowRatio => intervalArrow > 0f ? Mathf.Clamp01(intervalArrowTimer / intervalArrow) : 0f;
    public bool IsAimingArrow => isAiming;

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
        sr = GetComponent<SpriteRenderer>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.priority = 0;   // 効果音が他の音に押し出されないよう最優先にする

        // 初回再生時の読み込み待ちで音が遅れないよう、先に読み込んでおく
        if (jumpSound != null && jumpSound.loadState == AudioDataLoadState.Unloaded)
            jumpSound.LoadAudioData();

        rb.freezeRotation = true;   // コライダーが地面との摩擦で転がるのを防ぐ
        rb.gravityScale = gravityScale;
        jumpCount = maxJumpCount;
        respawnPoint = transform.position;
        CreateFlame();
        CreateAimGuide();

        // 🌟【追加】最初は弓矢を非表示にしておく
        if (bowAndArrow != null)
        {
            bowBaseLocalPosition = bowAndArrow.transform.localPosition;
            bowBaseLocalPosition.x = Mathf.Abs(bowBaseLocalPosition.x);
            bowBaseLocalScale = bowAndArrow.transform.localScale;
            bowAndArrow.SetActive(false);
        }
    }

    // 構えている間だけ表示する、射出方向の薄い矢印
    void CreateAimGuide()
    {
        if (!showAimGuide) return;

        var guideObj = new GameObject("AimGuide");
        guideObj.transform.SetParent(transform, false);

        aimGuide = guideObj.AddComponent<LineRenderer>();
        aimGuide.useWorldSpace = true;
        aimGuide.positionCount = 5;   // 軸2点 + 矢じり3点
        aimGuide.widthMultiplier = aimGuideWidth;
        aimGuide.numCapVertices = 2;
        aimGuide.startColor = aimGuideColor;
        aimGuide.endColor = aimGuideColor;
        aimGuide.sortingOrder = 50;

        Material mat = aimGuideMaterial;
        if (mat == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) mat = new Material(shader);
        }
        if (mat != null) aimGuide.sharedMaterial = mat;

        aimGuide.enabled = false;
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

        // 🌟【追加】死亡時に弓を隠す
        CancelAiming();

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
        jumpSoundPlayedEarly = false;
        coyoteTimer = 0f;
        isRising = false;
        canBoost = false;
        jumpCount = maxJumpCount;
        groundJumpSpent = false;

        jumpable = false;
        canControl = true;
        isLaunched = false;

        // リスポーン直後は一瞬だけ操作を受け付けない
        controlLockTimer = respawnControlLockTime;

        spriteRenderer.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        isDead = false;

        // 🌟【追加】リスポーン時に弓をリセット
        CancelAiming();
        intervalArrowTimer = 0f;

        OnRespawn?.Invoke(); // 既存のイベント呼び出しがあれば
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

        if (controlLockTimer > 0f) controlLockTimer -= Time.deltaTime;
        if (intervalArrowTimer > 0f) intervalArrowTimer -= Time.deltaTime;
        bool inputLocked = IsInputLocked;

        // canControlがtrueの時、または空中・ジャンプ可能な時に入力を受付
        if (!inputLocked && (canControl || isGrounded || jumpable || jumpCount > 0))
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            {
                jumpBuffer = jumpBufferTime;

                // ジャンプ音はFixedUpdate(物理更新)を待たず、押したフレームですぐ鳴らす
                if (CanStartJump() && !jumpSoundPlayedEarly)
                {
                    PlayJumpSound();
                    jumpSoundPlayedEarly = true;
                }
            }
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
        bool flameHeld = !inputLocked && (kb.sKey.isPressed || kb.downArrowKey.isPressed);
        if (flame.activeSelf != flameHeld)
            flame.SetActive(flameHeld);

        // 🌟【追加】Mキーでの弓矢の操作 (操作可能な時のみ)
        if (canControl && !inputLocked)
            HandleBowInput(kb);
        else if (isAiming)
            CancelAiming();
    }

    bool CanStartJump() => isGrounded || coyoteTimer > 0f || jumpCount > 0 || jumpable;

    void PlayJumpSound()
    {
        if (jumpSound != null) audioSource.PlayOneShot(jumpSound);
    }

    // ===== 火の弓矢 =====
    // M押す → chargeArrow / M長押し → targetArrow（射出方向が上下に往復） / M離す → shotArrow で発射
    void HandleBowInput(Keyboard kb)
    {
        // Mキーを押した瞬間：構えてタイマーをリセット（インターバル中は構えられない）
        if (kb.mKey.wasPressedThisFrame && !isAiming && intervalArrowTimer <= 0f)
        {
            isAiming = true;
            aimTimer = 0f;
            currentAimAngle = 0f;
            if (bowAndArrow != null) bowAndArrow.SetActive(true);
            if (aimGuide != null) aimGuide.enabled = true;
            SetAnimTrigger("chargeArrow");
            UpdateBowPose();
            return;
        }

        if (!isAiming) return;

        // Mキーを押し続けている間：往復回転 ＋ タイムアウト処理
        if (kb.mKey.isPressed)
        {
            aimTimer += Time.deltaTime;

            if (aimTimer >= bowTimeout)
            {
                // 時間切れなら構えを解除して消す（発射はしない）
                CancelAiming();
                return;
            }

            SetAnimBool("targetArrow", true);

            // 水平(0度)から始まって、上下に弧を描くように往復する
            currentAimAngle = Mathf.Sin(aimTimer * swingSpeed) * swingAngle;
            UpdateBowPose();
        }

        // Mキーを離した瞬間：発射
        if (kb.mKey.wasReleasedThisFrame || !kb.mKey.isPressed)
        {
            UpdateBowPose();
            Vector2 dir = AimDirection();
            Vector2 spawnPos = ArrowSpawnPosition(dir);

            HideBow();
            SetAnimBool("targetArrow", false);
            SetAnimTrigger("shotArrow");

            ShootArrow(spawnPos, dir);
            intervalArrowTimer = intervalArrow;
        }
    }

    float FacingSign => (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;

    // 実際に矢が飛ぶ方向（ワールド座標）
    Vector2 AimDirection()
    {
        float rad = currentAimAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad) * FacingSign, Mathf.Sin(rad));
    }

    Vector2 BowWorldPosition()
    {
        Vector2 local = new Vector2(bowBaseLocalPosition.x * FacingSign, bowBaseLocalPosition.y);
        return transform.TransformPoint(local);
    }

    Vector2 ArrowSpawnPosition(Vector2 dir) => BowWorldPosition() + dir * arrowSpawnDistance;

    // 弓・発射位置・ガイドの矢印を、今の向きと角度に合わせる
    void UpdateBowPose()
    {
        Vector2 dir = AimDirection();
        float worldAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float facing = FacingSign;

        if (bowAndArrow != null)
        {
            Transform bow = bowAndArrow.transform;
            bow.localPosition = new Vector3(bowBaseLocalPosition.x * facing, bowBaseLocalPosition.y, bow.localPosition.z);
            bow.rotation = Quaternion.Euler(0f, 0f, worldAngle);
            // 左向きのときに弓が上下逆さまにならないようにする
            bow.localScale = new Vector3(bowBaseLocalScale.x, Mathf.Abs(bowBaseLocalScale.y) * facing, bowBaseLocalScale.z);
        }

        Vector2 spawnPos = ArrowSpawnPosition(dir);
        if (arrowSpawnPoint != null)
        {
            arrowSpawnPoint.position = new Vector3(spawnPos.x, spawnPos.y, arrowSpawnPoint.position.z);
            arrowSpawnPoint.rotation = Quaternion.Euler(0f, 0f, worldAngle);
        }

        if (aimGuide != null)
        {
            Vector2 tip = spawnPos + dir * aimGuideLength;
            float head = Mathf.Min(0.3f, aimGuideLength * 0.3f);
            Vector2 back = tip - dir * head;
            Vector2 side = new Vector2(-dir.y, dir.x) * head * 0.6f;
            aimGuide.SetPosition(0, spawnPos);
            aimGuide.SetPosition(1, tip);
            aimGuide.SetPosition(2, back + side);
            aimGuide.SetPosition(3, tip);
            aimGuide.SetPosition(4, back - side);
        }
    }

    void HideBow()
    {
        isAiming = false;
        if (bowAndArrow != null) bowAndArrow.SetActive(false);
        if (aimGuide != null) aimGuide.enabled = false;
    }

    // 撃たずに構えをやめる（死亡・リスポーン・大砲に入ったときなど）
    void CancelAiming()
    {
        if (isAiming) SetAnimBool("targetArrow", false);
        HideBow();
        aimTimer = 0f;
    }

    // 🌟【追加】矢を発射する処理
    void ShootArrow(Vector2 spawnPos, Vector2 dir)
    {
        if (arrowPrefab == null) return;

        float worldAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        GameObject firedArrow = Instantiate(arrowPrefab, spawnPos, Quaternion.Euler(0f, 0f, worldAngle));

        // 火の矢の当たり判定（松明・コウモリ・導火線・綱）。プレハブに付いていなければ自動で付ける
        FireArrow fireArrow = firedArrow.GetComponent<FireArrow>();
        if (fireArrow == null) fireArrow = firedArrow.AddComponent<FireArrow>();
        fireArrow.SetOwner(gameObject);

        // 自分の矢がプレイヤー自身に当たらないようにする
        foreach (var arrowCol in firedArrow.GetComponentsInChildren<Collider2D>())
        {
            foreach (var myCol in GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(arrowCol, myCol);
        }

        Rigidbody2D rbArrow = firedArrow.GetComponent<Rigidbody2D>();
        if (rbArrow != null)
        {
            rbArrow.linearVelocity = dir * shootForce;
        }

        if (arrowShotSound != null) audioSource.PlayOneShot(arrowShotSound);
    }

    // Animatorにそのパラメータがあるときだけ設定する（アニメーション未作成でも警告が出ないように）
    bool HasAnimParam(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.type == type && p.name == paramName) return true;
        }
        return false;
    }

    void SetAnimTrigger(string paramName)
    {
        if (HasAnimParam(paramName, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(paramName);
    }

    void SetAnimBool(string paramName, bool value)
    {
        if (HasAnimParam(paramName, AnimatorControllerParameterType.Bool) && animator.GetBool(paramName) != value)
            animator.SetBool(paramName, value);
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

        // 大砲の中に入っている時や硬直中(canControl == false)、リスポーン直後は移動入力をパスする
        if (canControl && !IsInputLocked)
        {
            HorizontalMove();
        }

        Jump();
        if (jumpBuffer <= 0f) jumpSoundPlayedEarly = false;   // 先に鳴らしたがジャンプしなかった場合のリセット
        KillUnwantedLift();
        UpdateAppearance();
    }

    bool CheckGrounded()
    {
        if (rb.linearVelocity.y > 0.1f) return false;

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
            if (hit != col && !hit.usedByEffector && !hit.CompareTag("Box"))
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

        // 1. 左右の入力を取得
        float moveInput = 0f;
        if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) moveInput = -1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) moveInput = 1f;

        // 2. 壁へのめり込み・張り付きチェック
        if (moveInput != 0f && IsTouchingWall(moveInput))
        {
            moveInput = 0f;
        }

        // 3. 移動速度の適用
        float targetVx = moveInput * moveSpeed;
        float vx = targetVx;
        if (isLaunched && !isGrounded)
        {
            // 大砲で飛ばされている間は、発射の勢い（慣性）を残したまま少しずつ入力方向に寄せる
            vx = Mathf.MoveTowards(rb.linearVelocity.x, targetVx, launchAirControl * Time.fixedDeltaTime);
        }
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        // 4. スプライト反転用に lastX を更新
        if (moveInput != 0f)
        {
            lastX = moveInput;
        }
    }

    void Jump()
    {
        var kb = Keyboard.current;
        bool held = kb != null && !IsInputLocked && (kb.upArrowKey.isPressed || kb.wKey.isPressed || kb.spaceKey.isPressed);

        if (jumpBuffer > 0f && CanStartJump())
        {
            // Updateで先に鳴らしていなければここで鳴らす（着地直前の先行入力など）
            if (!jumpSoundPlayedEarly) PlayJumpSound();
            jumpSoundPlayedEarly = false;

            float jumpPower = PowerForHeight(HeightForBlocks(minJumpBlocks));
            float vy = jumpPower;
            if (isLaunched)
            {
                // 大砲の強発射中は、発射の勢いを消さずにジャンプする
                float launchVy = Mathf.Max(rb.linearVelocity.y, 0f);
                vy = addJumpToLaunch ? launchVy + jumpPower : Mathf.Max(launchVy, jumpPower);
            }
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, vy);
            jumpStartY = rb.position.y;
            jumpTime = 0f;
            isRising = true;
            canBoost = true;
            jumpBuffer = 0f;
            coyoteTimer = 0f;

            if (!isLaunched)
            {
                canControl = true;
            }
            jumpable = false;

            if (!isGrounded && coyoteTimer <= 0f && jumpCount > 0)
            {
                jumpCount--;
            }
            groundJumpSpent = true;
            fireTimer = fireDuration;

            if (animator != null) animator.SetTrigger("Jump");
        }
    
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
                {
                    float boosted = PowerForHeight(remaining);
                    if (isLaunched) boosted = Mathf.Max(boosted, rb.linearVelocity.y);   // 発射の勢いを弱めない
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, boosted);
                }
            }
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

        // スプライトの反転 (lastX は HorizontalMove で更新されるようになります)
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