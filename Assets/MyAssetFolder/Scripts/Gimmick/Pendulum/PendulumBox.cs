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
            Debug.Log("Playerが接触しました！ normal.y: " + collision.contacts[0].normal.y);

            if (collision.contacts[0].normal.y < -0.5f)
            {
                collision.transform.SetParent(transform);
            }
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }
}