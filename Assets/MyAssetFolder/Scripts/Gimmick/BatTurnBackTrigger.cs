using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class BatTurnBackTrigger : MonoBehaviour
{
    void Reset() => ConfigureTrigger();

    void Awake() => ConfigureTrigger();

    void ConfigureTrigger()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.useFullKinematicContacts = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActiveAndEnabled) return;

        BatEnemy bat = other.GetComponentInParent<BatEnemy>();
        if (bat != null) bat.TurnBack();
    }

    void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
