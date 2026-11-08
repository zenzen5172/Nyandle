using System.Collections.Generic;
using UnityEngine;

// コウモリ：天井にぶら下がって待ち、プレイヤーが近づくと急降下して襲い、別の天井へ戻る。
// 動きは決まった軌道ではなく「速度＋加速度（ステアリング）」で計算するので、
// 勢いがついて行き過ぎたり、曲がりながら減速したりと生き物らしく動く。
public class BatEnemy : MonoBehaviour, IIgnitable
{
    [Header("ターゲット（プレイヤー）")]
    [SerializeField] Transform player;

    [Header("反応する距離（Sceneビューで選択すると赤い円で表示）")]
    [SerializeField] float triggerDistance = 6f;

    [Header("天井の待機ポイント（複数設定してください）")]
    [SerializeField] Transform[] ceilingPoints;

    [Header("次の攻撃までのインターバル（秒）")]
    [SerializeField] float cooldownTime = 2f;

    [Header("急降下（プレイヤーに向かって加速する）")]
    [SerializeField] float diveSpeed = 9f;          // 急降下の最高速度
    [SerializeField] float diveAcceleration = 30f;  // 曲がる・加速する強さ（大きいほどキビキビ）
    [SerializeField] float diveHoming = 1.5f;       // 急降下中にプレイヤーを追いかける度合い（0で最初に見た位置へ一直線）
    [SerializeField] float maxDiveTime = 2f;        // これ以上たったら諦めて天井へ戻る

    [Header("天井へ戻る飛び方")]
    [SerializeField] float flySpeed = 7f;           // 戻るときの最高速度
    [SerializeField] float flyAcceleration = 28f;   // 小さいほど大回りする（急降下の勢いが残ってU字が深くなる）
    [SerializeField] float arriveRadius = 1.5f;     // 天井にこの距離まで近づいたら減速し始める

    [Header("Playerの光に当たったときの弱体化")]
    [SerializeField] float hitTime = 7f; // この秒数、光に当たり続けると死ぬ
    [SerializeField] float lightRadius = 8f; // Playerの光が届く範囲
    [SerializeField] float recoverRate = 3f; // 光に当たっていないときの回復速度の倍率
    [SerializeField] float closeDamageMultiplier = 3f; // Playerに密着したときの最大ダメージ倍率（近いほどHitTimeが速く減る）
    [SerializeField] float hurtGlowIntensity = 6f; // 黒いスプライトでも赤く光らせるためのオーバーブライト値（HDR）

    [Header("火の矢が当たったときのダメージ（HitTimeに何秒ぶん加算するか）")]
    [SerializeField] float arrowDamage = 3f;
    [SerializeField] float arrowNoRecoverTime = 2f; // 矢が当たってから回復しない時間（光の外から撃っても連続ヒットでダメージが溜まるように）

    [Header("火を怖がる（IgnoreFireの scaresBats / batFearRadius の範囲を避ける）")]
    [SerializeField] bool fearFire = true;
    [SerializeField] bool fearPlayerFire = false;  // Playerが燃えているときも怖がるか
    [SerializeField] float playerFireFearRadius = 3f;

    [Header("火に突っ込んだときの怯み（熱っ！）（IgnoreFireの batDamageRadius に入ったとき）")]
    [SerializeField] float fireHitDamage = 2f;      // 怯んだときに一度だけ受けるダメージ（HitTimeに加算）
    [SerializeField] float flinchKnockback = 9f;    // 火と反対方向へ弾かれる速さ
    [SerializeField] bool torchContinuousDamage = false; // 以前の「火の近くにいる間じわじわダメージ」も使うか

    private SpriteRenderer spriteRenderer;
    private Player playerComponent;
    private Color originalColor;
    [SerializeField ]private float hitTimer = 0f; // 0〜hitTimeで蓄積し、hitTimeに達すると死ぬ
    private float arrowNoRecoverTimer = 0f;
    private float flashTimer = 0f;        // 怯んだ瞬間に真っ赤に光らせる時間
    private float flinchImmuneTimer = 0f; // 怯んだ直後は連続で怯まない
    private const float FlashTime = 0.3f;       // 怯んだときに赤くなる時間（秒）
    private const float FlinchImmuneTime = 1f;  // 怯んだ後、再び怯むまでの時間（秒）

    // コウモリの現在の状態
    private enum State { Perched, Diving, Returning }
    private State currentState = State.Perched;

    private Vector2 position;       // 現在位置
    private Vector2 velocity;
    private float stateTimer;       // 今の状態になってからの時間
    private float cooldownTimer;    // 天井で次に攻撃できるまでの時間
    private Vector2 diveTarget;     // 急降下で狙う地点
    private Vector2 diveStart;
    private Vector2 homePosition;   // 戻る先（天井）
    private Transform currentCeilingPoint;

    // 実際に飛んだ軌跡（Sceneビューに表示する用）
    private readonly Queue<Vector3> trail = new Queue<Vector3>();
    private const int TrailLength = 90;

    void Start()
    {
        // プレイヤーが未設定ならTagから自動で探す
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }
        if (player != null) playerComponent = player.GetComponent<Player>();

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

        position = transform.position;
        homePosition = position;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        stateTimer += dt;

        // 火に突っ込んだら「熱っ！」と怯む（いつでも）
        if (flinchImmuneTimer > 0f) flinchImmuneTimer -= dt;
        if (flinchImmuneTimer <= 0f && TryGetTouchingFire(out Vector2 fireCenter))
        {
            if (!StartFlinch(fireCenter)) return; // ダメージで死んだ
        }

        switch (currentState)
        {
            case State.Perched:   UpdatePerched(dt); break;
            case State.Diving:    UpdateDiving(dt); break;
            case State.Returning: UpdateReturning(dt); break;
        }

        transform.position = new Vector3(position.x, position.y, transform.position.z);

        RecordTrail();
        UpdateLightDamage();
    }

    // ===== 状態ごとの動き =====

    private void UpdatePerched(float dt)
    {
        position = homePosition;
        velocity = Vector2.zero;
        if (cooldownTimer > 0f) cooldownTimer -= dt;

        // 近くで火がついたら、火から離れた天井へ逃げる
        if (IsInFearZone(position))
        {
            Transform safe = PickSafeCeilingPoint(excludeCurrent: true);
            if (safe != null && safe != currentCeilingPoint)
            {
                velocity = Vector2.down * 2f; // ぶら下がった所から落ちるように飛び立つ
                FlyHome(safe);
            }
            return;
        }

        if (cooldownTimer <= 0f && player != null
            && Vector2.Distance(position, player.position) <= triggerDistance
            && !IsTargetProtectedByFire(player.position))
        {
            diveStart = position;
            diveTarget = player.position;
            velocity = Vector2.down * 2f; // ぶら下がった所から落ちるように飛び立つ
            SetState(State.Diving);
        }
    }

    private void UpdateDiving(float dt)
    {
        // 少しだけプレイヤーを追いかける
        if (player != null && diveHoming > 0f)
            diveTarget = Vector2.Lerp(diveTarget, player.position, diveHoming * dt);

        Vector2 toTarget = diveTarget - position;
        Steer(toTarget.normalized * diveSpeed, diveAcceleration, dt);
        Integrate(dt);

        // 目標に着いた／通り過ぎた／時間切れ → 勢いを残したまま天井へ戻り始める（自然にU字になる）
        bool reached = toTarget.magnitude < 1.2f; // 手前で切り替えても勢いでプレイヤーの所まで届く
        bool passed = stateTimer > 0.2f && Vector2.Dot(velocity, diveTarget - position) < 0f;
        if (reached || passed || stateTimer > maxDiveTime)
        {
            FlyHome(PickSwoopEndPoint(diveStart, diveTarget));
        }
    }

    private void UpdateReturning(float dt)
    {
        Vector2 toHome = homePosition - position;
        float dist = toHome.magnitude;

        // 近づくほど減速して、ふわっと天井に止まる
        float speed = flySpeed * Mathf.Clamp01(dist / arriveRadius);
        Steer(toHome.normalized * speed, flyAcceleration, dt);
        Integrate(dt);

        // 着いた（または長く迷いすぎた）ら天井にぶら下がる
        if ((dist < 0.3f && velocity.magnitude < 2.5f) || stateTimer > 6f)
        {
            position = homePosition;
            velocity = Vector2.zero;
            cooldownTimer = cooldownTime;
            SetState(State.Perched);
        }
    }

    // 目標の速度に向けて、最大 accel の強さで今の速度を変える
    private void Steer(Vector2 desiredVelocity, float accel, float dt)
    {
        Vector2 change = Vector2.ClampMagnitude(desiredVelocity - velocity, accel * dt);
        velocity += change;
    }

    private void Integrate(float dt)
    {
        position += velocity * dt;
    }

    private void FlyHome(Transform perch)
    {
        if (perch != null)
        {
            currentCeilingPoint = perch;
            homePosition = perch.position;
        }
        SetState(State.Returning);
    }

    private void SetState(State next)
    {
        currentState = next;
        stateTimer = 0f;
    }

    // ===== 火 =====

    // 「熱っ！」：ダメージを受けて赤くなり、火と反対方向へ弾かれる（速度を変えるだけ）。
    // そのまま安全な天井へ戻るので、ステアリングで自然に向きを立て直す。死んだら false を返す
    private bool StartFlinch(Vector2 fireCenter)
    {
        flinchImmuneTimer = FlinchImmuneTime;
        flashTimer = FlashTime;
        hitTimer = Mathf.Min(hitTimer + fireHitDamage, hitTime);
        arrowNoRecoverTimer = arrowNoRecoverTime;
        if (hitTimer >= hitTime)
        {
            Destroy(gameObject);
            return false;
        }

        Vector2 away = position - fireCenter;
        velocity = (away.sqrMagnitude > 0f ? away.normalized : Vector2.up) * flinchKnockback;
        FlyHome(PickSafeCeilingPoint(excludeCurrent: false));
        return true;
    }

    // 火が付いた松明の batDamageRadius 以内にいるか（いれば一番近い火の位置を返す）
    private bool TryGetTouchingFire(out Vector2 center)
    {
        center = Vector2.zero;
        float best = float.MaxValue;
        foreach (IgnoreFire torch in IgnoreFire.All)
        {
            if (!torch.damageBats || !torch.IsLit || torch.batDamageRadius <= 0f) continue;
            float d = Vector2.Distance(position, torch.LightCenter);
            if (d <= torch.batDamageRadius && d < best)
            {
                best = d;
                center = torch.LightCenter;
            }
        }
        return best < float.MaxValue;
    }

    // 指定位置が、怖い火の範囲内か
    private bool IsInFearZone(Vector2 pos)
    {
        if (fearFire)
        {
            foreach (IgnoreFire fire in IgnoreFire.All)
            {
                if (!fire.scaresBats || !fire.IsLit) continue;
                if (Vector2.Distance(pos, fire.LightCenter) < fire.batFearRadius) return true;
            }
        }
        if (IsPlayerFireScary() && Vector2.Distance(pos, player.position) < playerFireFearRadius) return true;
        return false;
    }

    // 突っ込む先（プレイヤーの位置）が怖い火の範囲内なら攻撃しない
    // （経路はチェックしない＝途中で火に突っ込んだら「熱っ！」と怯む）
    private bool IsTargetProtectedByFire(Vector2 target)
    {
        // Playerが燃えていて怖い場合は、Playerを襲わない
        if (IsPlayerFireScary()) return true;
        return IsInFearZone(target);
    }

    private bool IsPlayerFireScary()
    {
        return fearPlayerFire && player != null && playerComponent != null && playerComponent.IsOnFire();
    }

    // ===== 天井ポイントの選び方 =====

    // 火の範囲にない天井ポイントをランダムに選ぶ（安全な所がなければ今のポイント）
    private Transform PickSafeCeilingPoint(bool excludeCurrent)
    {
        if (ceilingPoints == null || ceilingPoints.Length == 0) return null;
        var safe = new List<Transform>();
        foreach (Transform point in ceilingPoints)
        {
            if (point == null || (excludeCurrent && point == currentCeilingPoint)) continue;
            if (!IsInFearZone(point.position)) safe.Add(point);
        }
        if (safe.Count > 0) return safe[Random.Range(0, safe.Count)];
        return currentCeilingPoint;
    }

    // 急降下の後に戻る天井：火の範囲外で、プレイヤーを通り抜けた先（スタートと反対側）の近い所を優先
    private Transform PickSwoopEndPoint(Vector2 start, Vector2 bottom)
    {
        if (ceilingPoints == null || ceilingPoints.Length == 0) return null;
        float dirIn = Mathf.Sign(bottom.x - start.x);
        var ahead = new List<Transform>();
        foreach (Transform point in ceilingPoints)
        {
            if (point == null || point == currentCeilingPoint || IsInFearZone(point.position)) continue;
            if (dirIn == 0f || Mathf.Sign(point.position.x - bottom.x) == dirIn) ahead.Add(point);
        }
        if (ahead.Count == 0) return PickSafeCeilingPoint(excludeCurrent: true);

        // 遠すぎる天井へ一気に飛ばないよう、近い2つのどちらか
        ahead.Sort((a, b) => Mathf.Abs(a.position.x - bottom.x).CompareTo(Mathf.Abs(b.position.x - bottom.x)));
        return ahead[Random.Range(0, Mathf.Min(2, ahead.Count))];
    }

    // ===== ダメージ =====

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
        float torchMultiplier = torchContinuousDamage ? GetTorchDamageMultiplier() : 0f;
        if (torchMultiplier > 0f)
        {
            damageMultiplier += torchMultiplier;
            isLit = true;
        }

        if (arrowNoRecoverTimer > 0f) arrowNoRecoverTimer -= Time.deltaTime;
        if (flashTimer > 0f) flashTimer -= Time.deltaTime;

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
            // ダメージを受けたら FlashTime の間だけ真っ赤になる（マイクラ風）
            float t = flashTimer > 0f ? 1f : hitTimer / hitTime;
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

    // ===== Sceneビュー表示 =====

    private void RecordTrail()
    {
        if (currentState == State.Perched) return; // ぶら下がっている間は記録しない
        trail.Enqueue(transform.position);
        while (trail.Count > TrailLength) trail.Dequeue();
    }

    // 選択中、反応する距離（赤）・天井の待機ポイント（水色）・実際に飛んだ軌跡（黄色）をSceneビューに表示
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, triggerDistance);

        if (ceilingPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform point in ceilingPoints)
                if (point != null) Gizmos.DrawWireSphere(point.position, 0.3f);
        }

        if (trail.Count > 1)
        {
            Gizmos.color = Color.yellow;
            Vector3 prev = Vector3.zero;
            bool first = true;
            foreach (Vector3 p in trail)
            {
                if (!first) Gizmos.DrawLine(prev, p);
                prev = p;
                first = false;
            }
        }
    }
}
