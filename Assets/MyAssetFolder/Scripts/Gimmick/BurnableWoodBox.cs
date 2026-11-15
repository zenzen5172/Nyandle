using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class BurnableWoodBox : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float burnDuration = 1.5f;
    [SerializeField] Sprite flameSprite;
    [SerializeField] Material flameMaterial;

    Rigidbody2D body;
    SpriteRenderer box;
    SpriteRenderer flame;
    Vector2 initialPosition;
    float initialRotation;
    Color initialColor;
    Vector3 flameSize;
    float burnTime;
    bool isBurning;
    bool isBurned;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        box = GetComponent<SpriteRenderer>();
        initialPosition = body.position;
        initialRotation = body.rotation;
        initialColor = box.color;
        Player.OnRespawn += ResetBox;
    }

    void OnDestroy() => Player.OnRespawn -= ResetBox;

    void OnCollisionEnter2D(Collision2D collision) => TryIgniteFromJumpFire(collision);

    void OnCollisionStay2D(Collision2D collision) => TryIgniteFromJumpFire(collision);

    void TryIgniteFromJumpFire(Collision2D collision)
    {
        if (isBurning || isBurned) return;

        Rigidbody2D otherBody = collision.rigidbody;
        if (otherBody != null && otherBody.TryGetComponent(out Player player)
            && player.CanBurnWoodBoxes && player.IsJumpFireActive)
        {
            Ignite();
        }
    }

    public void Ignite()
    {
        if (!isActiveAndEnabled || isBurning || isBurned) return;

        if (flame == null && flameSprite != null && box.sprite != null)
        {
            flame = new GameObject("Box Flame").AddComponent<SpriteRenderer>();
            flame.transform.SetParent(transform, false);
            Bounds bounds = box.sprite.bounds;
            flame.transform.localPosition = new Vector3(bounds.center.x, bounds.max.y * 0.55f, bounds.center.z);
            flame.sprite = flameSprite;
            if (flameMaterial != null) flame.sharedMaterial = flameMaterial;
            flame.sortingLayerID = box.sortingLayerID;
            flame.sortingOrder = box.sortingOrder + 1;
            flameSize = Vector3.one * (bounds.size.x / flameSprite.bounds.size.x);
        }

        burnTime = 0f;
        isBurning = true;
        box.color = initialColor * new Color(1f, 0.56f, 0.27f);
        if (flame != null)
        {
            flame.transform.localScale = flameSize * 0.72f;
            flame.enabled = true;
        }
    }

    void Update()
    {
        if (!isBurning) return;

        burnTime += Time.deltaTime;
        float progress = Mathf.Clamp01(burnTime / burnDuration);
        box.color = Color.Lerp(initialColor * new Color(1f, 0.56f, 0.27f),
            initialColor * new Color(0.28f, 0.2f, 0.16f), progress);
        if (flame != null)
        {
            float flicker = 1f + Mathf.Sin(burnTime * 31f) * 0.1f;
            flame.transform.localScale = flameSize * (Mathf.Lerp(0.72f, 1.15f, progress) * flicker);
        }

        if (progress < 1f) return;

        isBurning = false;
        isBurned = true;
        body.simulated = false;
        box.enabled = false;
        if (flame != null) flame.enabled = false;
    }

    void ResetBox()
    {
        if (!isBurning && !isBurned) return;

        isBurning = false;
        isBurned = false;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.position = initialPosition;
        body.rotation = initialRotation;
        body.simulated = true;
        box.color = initialColor;
        box.enabled = true;
        if (flame != null) flame.enabled = false;
    }
}
