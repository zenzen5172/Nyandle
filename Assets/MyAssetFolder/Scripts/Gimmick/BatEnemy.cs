using System.Collections;
using UnityEngine;

public class BatEnemy : MonoBehaviour, IIgnitable
{
    [Header("ターゲット（プレイヤー）")]
    [SerializeField] Transform player;

    [Header("反応する距離")]
    [SerializeField] float triggerDistance = 6f;

    [Header("移動にかかる時間（秒）")]
    [SerializeField] float swoopDuration = 1f;

    [Header("弧の膨らみ（マイナスで下膨れ、プラスで上膨れ）")]
    [SerializeField] float swoopArc = -2f; // 突っ込むときのカーブ
    [SerializeField] float returnArc = 2f;  // 天井へ帰るときのカーブ

    [Header("天井の待機ポイント（複数設定してください）")]
    [SerializeField] Transform[] ceilingPoints;

    [Header("次の攻撃までのインターバル（秒）")]
    [SerializeField] float cooldownTime = 2f;

    [Header("Playerの光に当たったときの弱体化")]
    [SerializeField] float hitTime = 7f; // この秒数、光に当たり続けると死ぬ
    [SerializeField] float lightRadius = 8f; // Playerの光が届く範囲
    [SerializeField] float recoverRate = 3f; // 光に当たっていないときの回復速度の倍率
    [SerializeField] float closeDamageMultiplier = 3f; // Playerに密着したときの最大ダメージ倍率（近いほどHitTimeが速く減る）
    [SerializeField] float hurtGlowIntensity = 6f; // 黒いスプライトでも赤く光らせるためのオーバーブライト値（HDR）

    [Header("火の矢が当たったときのダメージ（HitTimeに何秒ぶん加算するか）")]
    [SerializeField] float arrowDamage = 3f;
    [SerializeField] float arrowNoRecoverTime = 2f; // 矢が当たってから回復しない時間（光の外から撃っても連続ヒットでダメージが溜まるように）

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    [SerializeField ]private float hitTimer = 0f; // 0〜hitTimeで蓄積し、hitTimeに達すると死ぬ
    private float arrowNoRecoverTimer = 0f;

    private Vector2 startPos;
    private Vector2 targetPos;
    private Transform currentCeilingPoint;

    // コウモリの現在の状態
    private enum State { Idle, Swooping, Returning, Cooldown }
    private State currentState = State.Idle;

    void Start()
    {
        // プレイヤーが未設定ならTagから自動で探す
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        // 最初の天井ポイントを現在地に設定
        if (ceilingPoints != null && ceilingPoints.Length > 0)
        {
            currentCeilingPoint = ceilingPoints[0];
            transform.position = currentCeilingPoint.position;
        }
        else
        {
            Debug.LogWarning("天井の待機ポイント (Ceiling Points) が設定されていません！");
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        // 待機状態 ＆ プレイヤーが存在する ＆ プレイヤーとの距離が近い場合
        if (currentState == State.Idle && player != null)
        {
            if (Vector2.Distance(transform.position, player.position) <= triggerDistance)
            {
                StartCoroutine(SwoopAttack());
            }
        }

        UpdateLightDamage();
    }

    // Playerの光（＝距離がLightRadius以内）に当たり続けるとダメージが蓄積し、
    // 当たっていない間は徐々に回復する
    private void UpdateLightDamage()
    {
        bool isLit = false;
        float damageMultiplier = 0f;

        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= lightRadius)
            {
                // Playerに近いほどHitTimeが速く減る（＝ダメージが速く蓄積する）
                float closeness = 1f - Mathf.Clamp01(distance / lightRadius);
                damageMultiplier += Mathf.Lerp(1f, closeDamageMultiplier, closeness);
                isLit = true;
            }
        }

        // 火が付いた松明の光（各松明の batDamageRadius 以内）でもダメージ
        float torchMultiplier = GetTorchDamageMultiplier();
        if (torchMultiplier > 0f)
        {
            damageMultiplier += torchMultiplier;
            isLit = true;
        }

        if (arrowNoRecoverTimer > 0f) arrowNoRecoverTimer -= Time.deltaTime;

        if (isLit)
        {
            hitTimer += Time.deltaTime * damageMultiplier;
        }
        else if (arrowNoRecoverTimer <= 0f)
        {
            hitTimer -= Time.deltaTime * recoverRate;
        }

        hitTimer = Mathf.Clamp(hitTimer, 0f, hitTime);

        if (spriteRenderer != null)
        {
            // 通常のColor.Lerpだと黒に近いスプライトを赤くできないため、
            // オーバーブライト（1を超える値）の赤をHDRで加算的に乗算して光らせる
            float t = hitTimer / hitTime;
            Color hdrRed = new Color(hurtGlowIntensity, 0f, 0f, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, hdrRed, t);
        }

        if (hitTimer >= hitTime)
        {
            Destroy(gameObject);
        }
    }

    // 範囲内にある、火が付いた松明すべてのダメージ倍率の合計（Playerの光と同じく、松明に近いほど強い）
    private float GetTorchDamageMultiplier()
    {
        float total = 0f;
        Vector2 pos = transform.position;
        foreach (IgnoreFire torch in IgnoreFire.All)
        {
            if (!torch.damageBats || !torch.IsLit || torch.batDamageRadius <= 0f) continue;

            float d = Vector2.Distance(pos, torch.LightCenter);
            if (d > torch.batDamageRadius) continue;

            float closeness = 1f - Mathf.Clamp01(d / torch.batDamageRadius);
            total += Mathf.Lerp(1f, closeDamageMultiplier, closeness) * torch.batDamageMultiplier;
        }
        return total;
    }

    // 火の矢が当たったときのダメージ（光と同じく hitTimer に蓄積し、HitTime に達したら死ぬ）
    public void Ignite(GameObject source)
    {
        hitTimer = Mathf.Min(hitTimer + arrowDamage, hitTime);
        arrowNoRecoverTimer = arrowNoRecoverTime;
        if (hitTimer >= hitTime)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator SwoopAttack()
    {
        // --- 1. プレイヤーの位置へ突っ込む（Swoop Down） ---
        currentState = State.Swooping;
        startPos = transform.position;
        targetPos = player.position; // 突っ込む目標地点（発見した瞬間のプレイヤーの位置）

        float timer = 0f;
        while (timer < swoopDuration)
        {
            timer += Time.deltaTime;
            float t = timer / swoopDuration;

            // 直線上の位置を計算
            Vector2 linearPos = Vector2.Lerp(startPos, targetPos, t);

            // サイン波を使って、直線に対して膨らみ（弧）を加える
            float arcOffset = Mathf.Sin(t * Mathf.PI) * swoopArc;

            transform.position = linearPos + new Vector2(0, arcOffset);
            yield return null; // 次のフレームまで待機
        }
        transform.position = targetPos; // 最終位置のズレ防止

        // --- 2. 天井の別の位置へ帰る（Swoop Up） ---
        currentState = State.Returning;
        startPos = transform.position;

        // ランダムな別の天井ポイントを探す
        Transform nextCeilingPoint = GetRandomCeilingPoint(currentCeilingPoint);
        targetPos = nextCeilingPoint.position;
        currentCeilingPoint = nextCeilingPoint;

        timer = 0f;
        while (timer < swoopDuration)
        {
            timer += Time.deltaTime;
            float t = timer / swoopDuration;

            Vector2 linearPos = Vector2.Lerp(startPos, targetPos, t);
            float arcOffset = Mathf.Sin(t * Mathf.PI) * returnArc; // 帰りは上に膨らむ

            transform.position = linearPos + new Vector2(0, arcOffset);
            yield return null;
        }
        transform.position = targetPos;

        // --- 3. インターバル（Cooldown） ---
        currentState = State.Cooldown;
        yield return new WaitForSeconds(cooldownTime);

        // --- 4. 攻撃可能状態（Idle）に戻る ---
        currentState = State.Idle;
    }

    // 今いるポイント「以外」からランダムに次の天井ポイントを選ぶ処理
    private Transform GetRandomCeilingPoint(Transform excludePoint)
    {
        if (ceilingPoints == null || ceilingPoints.Length == 0) return transform;
        if (ceilingPoints.Length == 1) return ceilingPoints[0];

        Transform picked;
        do
        {
            int index = Random.Range(0, ceilingPoints.Length);
            picked = ceilingPoints[index];
        } while (picked == excludePoint); // 同じポイントが選ばれたら引き直し

        return picked;
    }

    // --- プレイヤーに触れたときの処理（リスポーンさせる） ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player p = collision.GetComponent<Player>();
            if (p != null)
            {
                p.Respawn(); // プレイヤー側のリスポーン処理を呼び出す
            }
        }
    }
}