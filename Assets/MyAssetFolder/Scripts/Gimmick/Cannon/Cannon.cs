using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Cannon : MonoBehaviour
{
    [SerializeField] Transform cannonPoint; // 発射口
    [SerializeField] Vector2 cannonDirection = new Vector2(1, 1);

    [Header("吸い込み演出")]
    [SerializeField, Min(0.01f)] float entryDuration = 0.35f;

    [Header("大砲のパワー")]
    [SerializeField] float strongPower = 20f;
    [SerializeField] float weakPower = 5f;
    [SerializeField] float stunDuration = 1f;

    [Header("連携する導火線")]
    [SerializeField] CannonFuse fuse;

    Player passenger;
    bool isBusy;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isBusy || cannonPoint == null || collision.isTrigger || !collision.CompareTag("Player")) return;
        if (!collision.TryGetComponent(out Player player) || !player.TryEnterCannon(this)) return;

        passenger = player;
        isBusy = true;
        StartCoroutine(CannonSequence(player, fuse != null && fuse.isLit));
    }

    IEnumerator CannonSequence(Player player, bool isPowered)
    {
        Transform rider = player.transform;
        Vector3 startPosition = rider.position;
        Vector3 startScale = rider.localScale;
        Vector3 inside = Vector3.Lerp(cannonPoint.position, transform.position, 0.35f);
        bool launchRequested = false;

        for (float elapsed = 0f; elapsed < entryDuration; elapsed += Time.deltaTime)
        {
            if (!HasPassenger(player)) break;

            launchRequested |= SpacePressed();
            float progress = elapsed / entryDuration;
            float pull = progress * progress;
            rider.position = Vector3.Lerp(startPosition, inside, pull);
            rider.localScale = Vector3.Lerp(startScale, startScale * 0.08f, progress);
            yield return null;
        }

        if (HasPassenger(player))
        {
            rider.position = inside;
            player.HideInCannon();

            while (HasPassenger(player) && (isPowered ? fuse != null && fuse.isLit : !launchRequested && !SpacePressed()))
                yield return null;
        }

        if (HasPassenger(player))
        {
            if (isPowered && fuse != null) fuse.CannonEntered();

            float power = isPowered ? strongPower : weakPower;
            float controlDelay = isPowered ? stunDuration : 0f;
            player.LeaveCannon(cannonPoint.position, cannonDirection * power, controlDelay);
            passenger = null;
            yield return new WaitForSeconds(0.5f);
        }

        passenger = null;
        isBusy = false;
    }

    bool HasPassenger(Player player) => player != null && player.IsInCannon(this);

    static bool SpacePressed() => !AllOverCameraManeger.IsActive && Keyboard.current?.spaceKey.wasPressedThisFrame == true;

    void OnDisable()
    {
        StopAllCoroutines();
        if (HasPassenger(passenger))
            passenger.LeaveCannon(cannonPoint.position, Vector2.zero, 0f);

        passenger = null;
        isBusy = false;
    }
}
