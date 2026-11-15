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

    [Header("強発射後の操作不能時間（秒）")]
    [SerializeField] float stunDuration = 1f;

    [Header("連携する導火線")]
    [SerializeField] CannonFuse fuse;

    [Header("音声")]
    public AudioClip shotSound;
    public AudioClip weakShotSound;
    AudioSource audioSource;

    Player passenger;
    bool isBusy;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

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

            launchRequested |= LaunchPressed();
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

            while (HasPassenger(player) && (isPowered ? fuse != null && fuse.isLit : !launchRequested && !LaunchPressed()))
                yield return null;
        }

        if (HasPassenger(player))
        {
            if (isPowered)
            {
                if (fuse != null) fuse.CannonEntered();
                if (shotSound != null && audioSource != null)
                    audioSource.PlayOneShot(shotSound);
            }
            else
            {
                if (weakShotSound != null && audioSource != null)
                    audioSource.PlayOneShot(weakShotSound);
            }

            float power = isPowered ? strongPower : weakPower;
            float controlDelay = isPowered ? stunDuration : 0.15f;
            player.LeaveCannon(cannonPoint.position, cannonDirection * power, controlDelay);
            passenger = null;
            yield return new WaitForSeconds(1f);
        }

        passenger = null;
        isBusy = false;
    }

    bool HasPassenger(Player player) => player != null && player.IsInCannon(this);

    static bool LaunchPressed()
    {
        if (AllOverCameraManeger.IsActive) return false;
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (HasPassenger(passenger))
            passenger.LeaveCannon(cannonPoint.position, Vector2.zero, 0f);

        passenger = null;
        isBusy = false;
    }
}
