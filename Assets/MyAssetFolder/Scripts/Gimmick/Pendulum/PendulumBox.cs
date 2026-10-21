using UnityEngine;

public class PendulumPlatform : MonoBehaviour
{
    private Transform playerTransform;
    private Vector3 lastPosition;

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (playerTransform != null)
        {
            Vector3 deltaPosition = transform.position - lastPosition;
            playerTransform.position += deltaPosition;
        }
        lastPosition = transform.position;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 上から乗った場合のみターゲットに設定
            if (collision.contacts[0].normal.y < -0.5f)
            {
                playerTransform = collision.transform;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (playerTransform == collision.transform)
            {
                playerTransform = null;
            }
        }
    }
}