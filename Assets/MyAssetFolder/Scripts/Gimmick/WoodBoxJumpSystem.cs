using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Player), typeof(Rigidbody2D))]
[DefaultExecutionOrder(100)]
public class WoodBoxJumpSystem : MonoBehaviour
{
    const float WallClearance = 0.01f;
    // 頭の上に箱が「乗っている」と見なす、頭頂から上の許容量
    const float HeadCarryReach = 0.1f;
    // 頭頂に少し沈んで乗っている箱も拾えるようにする、頭頂から下の許容量
    const float HeadCarryOverlap = 0.2f;

    Player player;
    Rigidbody2D playerBody;
    Collider2D playerCollider;
    Rigidbody2D carriedBox;
    BoxCollider2D carriedCollider;
    SpriteRenderer boxRenderer;
    Vector3 carryOffset;
    Vector2 boxCenterOffset;
    Vector2 boxSize;
    float boxAngle;
    readonly List<RaycastHit2D> wallHits = new();
    readonly List<Collider2D> headHits = new();
    readonly List<Joint2D> boxJoints = new();
    ContactFilter2D wallFilter;
    Vector2 previousPosition;
    float contactHeight;
    float upwardSpeedBeforePhysics;
    // 壁や天井に当たって離した箱を、そのジャンプ中に掴み直さないための記録
    Rigidbody2D releasedBox;
    bool ignoreReleasedBox;

    void Awake()
    {
        player = GetComponent<Player>();
        playerBody = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        wallFilter = new ContactFilter2D { useTriggers = false };
    }

    void OnEnable() => Player.OnRespawn += ReleaseBox;

    void OnDisable()
    {
        Player.OnRespawn -= ReleaseBox;
        ReleaseBox();
    }

    void FixedUpdate()
    {
        // 衝突で上向きの速度が消える前に、今回のジャンプ速度を覚えておく。
        upwardSpeedBeforePhysics = player.IsRisingFromJump && playerBody.simulated
            ? playerBody.linearVelocity.y : 0f;

        // 上昇が終わったら、離した箱を掴み直さない制限も解除する。
        if (!player.IsRisingFromJump) ignoreReleasedBox = false;

        // 接地したまま頭に箱を乗せていると、ジャンプしても新しい衝突は起きない。
        // 物理ステップで箱が弾かれる前に、ジャンプ開始時点で掴んでおく。
        if (carriedBox == null && upwardSpeedBeforePhysics > 0f && player.IsGrounded)
            TryCarryBoxOnHead();

        UpdateCarriedBox();
    }

    // 物理ステップ後、描画前に箱を追従させる。
    void LateUpdate() => UpdateCarriedBox();

    void UpdateCarriedBox()
    {
        if (carriedBox == null) return;

        if (!playerBody.simulated || !boxRenderer.enabled)
        {
            ReleaseBox();
            return;
        }

        Vector2 position = playerBody.position;
        bool reachedContactHeight = position.y <= contactHeight && playerBody.linearVelocity.y <= 0f;
        if (reachedContactHeight)
        {
            float progress = Mathf.InverseLerp(previousPosition.y, position.y, contactHeight);
            position = Vector2.Lerp(previousPosition, position, progress);
        }

        Vector2 movement = position - previousPosition;
        Vector2 allowedMovement = SweepBox(previousPosition, movement);
        PlaceBox(previousPosition + allowedMovement);
        // 箱だけが壁や天井に当たったら、プレイヤーの位置・速度はそのままで箱を離す。
        if (reachedContactHeight || (player.IsGrounded && playerBody.linearVelocity.y <= 0f)
            || allowedMovement.sqrMagnitude < movement.sqrMagnitude - 0.000001f)
        {
            ReleaseBoxWithoutOverlap();
            return;
        }

        previousPosition = position;
    }

    Vector2 SweepBox(Vector2 start, Vector2 movement)
    {
        float distance = movement.magnitude;
        if (distance <= 0.0001f) return movement;

        Vector2 direction = movement / distance;
        Physics2D.BoxCast(start + boxCenterOffset, boxSize, boxAngle, direction, wallFilter, wallHits, distance + WallClearance);
        float allowedDistance = distance;
        foreach (RaycastHit2D hit in wallHits)
        {
            Collider2D collider = hit.collider;
            if (collider.attachedRigidbody == playerBody || collider.attachedRigidbody == carriedBox || collider.usedByEffector)
                continue;

            float towardsWall = -Vector2.Dot(direction, hit.normal);
            if (towardsWall <= 0.001f) continue;

            float stopDistance = Mathf.Max(0f, hit.distance - WallClearance / towardsWall);
            allowedDistance = Mathf.Min(allowedDistance, stopDistance);
        }

        return direction * allowedDistance;
    }

    // 頭の上に乗っている木箱を探し、その場で掴む。
    void TryCarryBoxOnHead()
    {
        if (playerCollider == null) return;

        Bounds bounds = playerCollider.bounds;
        float bottom = bounds.max.y - HeadCarryOverlap;
        float top = bounds.max.y + HeadCarryReach;
        Vector2 probeCenter = new Vector2(bounds.center.x, (bottom + top) * 0.5f);
        Vector2 probeSize = new Vector2(bounds.size.x * 0.9f, top - bottom);
        int hitCount = Physics2D.OverlapBox(probeCenter, probeSize, 0f, wallFilter, headHits);

        Rigidbody2D foundBox = null;
        BoxCollider2D foundCollider = null;
        float nearestGap = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            if (headHits[i] is not BoxCollider2D boxCollider) continue;

            Rigidbody2D box = boxCollider.attachedRigidbody;
            if (box == null || box == playerBody || !box.simulated) continue;
            if (ignoreReleasedBox && box == releasedBox) continue;
            if (boxCollider.usedByEffector || !box.TryGetComponent<BurnableWoodBox>(out _)) continue;
            if (HasActiveJoint(box)) continue;

            // 箱が頭の真上にかかっていて、頭頂にほぼ接しているものだけを掴む。
            Bounds boxBounds = boxCollider.bounds;
            if (bounds.center.x < boxBounds.min.x || bounds.center.x > boxBounds.max.x) continue;
            float gap = boxBounds.min.y - bounds.max.y;
            if (gap < -HeadCarryOverlap || gap > HeadCarryReach || gap >= nearestGap) continue;

            nearestGap = gap;
            foundBox = box;
            foundCollider = boxCollider;
        }

        if (foundBox != null) CarryBox(foundBox, foundCollider);
    }

    // 木箱をプレイヤーに固定して、頭の上に乗せたまま運ぶ。
    bool CarryBox(Rigidbody2D box, BoxCollider2D boxCollider)
    {
        if (carriedBox != null || box == null || boxCollider == null || !box.simulated) return false;
        if (HasActiveJoint(box)) return false;

        SpriteRenderer renderer = box.GetComponent<SpriteRenderer>();
        if (renderer == null || !renderer.enabled) return false;

        carriedBox = box;
        carriedCollider = boxCollider;
        boxRenderer = renderer;
        releasedBox = null;
        previousPosition = playerBody.position;
        contactHeight = previousPosition.y;
        carryOffset = box.transform.position - new Vector3(previousPosition.x, previousPosition.y, transform.position.z);
        boxCenterOffset = (Vector2)carryOffset + (Vector2)boxCollider.transform.TransformVector(boxCollider.offset);
        Vector3 scale = boxCollider.transform.lossyScale;
        boxSize = Vector2.Scale(boxCollider.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
        boxAngle = boxCollider.transform.eulerAngles.z;
        box.linearVelocity = Vector2.zero;
        box.angularVelocity = 0f;
        box.simulated = false;
        return true;
    }

    // ロープなどで吊られている間は、頭で運ぶために物理演算を止めない。
    // 切断されてジョイントが無効になった箱は、通常どおり運べる。
    bool HasActiveJoint(Rigidbody2D box)
    {
        box.GetComponents(boxJoints);
        foreach (Joint2D joint in boxJoints)
            if (joint.isActiveAndEnabled) return true;
        return false;
    }

    void OnCollisionEnter2D(Collision2D collision) => TryCarryFromCollision(collision);

    // 頭に乗せたまま接地している箱は「触れ続けている」状態なので、ジャンプ時はStayでも掴む。
    void OnCollisionStay2D(Collision2D collision) => TryCarryFromCollision(collision);

    void TryCarryFromCollision(Collision2D collision)
    {
        if (!isActiveAndEnabled || carriedBox != null || upwardSpeedBeforePhysics <= 0f) return;
        Rigidbody2D box = collision.rigidbody;
        if (box == null || !box.simulated || !box.TryGetComponent<BurnableWoodBox>(out _)
            || collision.collider is not BoxCollider2D boxCollider) return;
        if (ignoreReleasedBox && box == releasedBox) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector2 normal = collision.GetContact(i).normal;
            if (normal.y > -0.7f || Vector2.Dot(normal, box.transform.up) > -0.8f) continue;
            if (!CarryBox(box, boxCollider)) return;

            // 木箱への衝突で失った上向きの速度を戻してジャンプを続ける。
            float verticalSpeed = upwardSpeedBeforePhysics + Physics2D.gravity.y * playerBody.gravityScale * Time.fixedDeltaTime;
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, Mathf.Max(0f, verticalSpeed));
            return;
        }
    }

    void PlaceBox(Vector2 playerPosition)
    {
        carriedBox.transform.position = new Vector3(playerPosition.x, playerPosition.y, transform.position.z) + carryOffset;
    }

    void ReleaseBoxWithoutOverlap()
    {
        BoxCollider2D box = carriedCollider;
        Vector2 carriedPlayerPosition = (Vector2)(carriedBox.transform.position - carryOffset);
        ReleaseBox();

        // 箱が止まった位置からプレイヤーの移動を調べ、箱に入る手前で止める。
        Vector2 movement = playerBody.position - carriedPlayerPosition;
        float distance = movement.magnitude;
        if (distance <= 0.0001f) return;

        Vector2 direction = movement / distance;
        playerCollider.Cast(carriedPlayerPosition, playerBody.rotation, direction, wallFilter, wallHits, distance);
        foreach (RaycastHit2D hit in wallHits)
        {
            if (hit.collider != box || Vector2.Dot(direction, hit.normal) >= 0f) continue;

            playerBody.position = carriedPlayerPosition + direction * Mathf.Max(0f, hit.distance - WallClearance);
            Vector2 velocity = playerBody.linearVelocity;
            playerBody.linearVelocity = velocity - Mathf.Min(0f, Vector2.Dot(velocity, hit.normal)) * hit.normal;
            return;
        }
    }

    void ReleaseBox()
    {
        if (carriedBox == null) return;

        // 同じジャンプ中に掴み直して暴れないよう、離した箱を覚えておく。
        releasedBox = carriedBox;
        ignoreReleasedBox = player != null && player.IsRisingFromJump;

        carriedBox.position = carriedBox.transform.position;
        if (boxRenderer.enabled) carriedBox.simulated = true;
        carriedBox = null;
        carriedCollider = null;
        boxRenderer = null;
    }

}
