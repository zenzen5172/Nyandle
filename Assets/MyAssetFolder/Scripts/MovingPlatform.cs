using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] float speed = 1.5f;
    [SerializeField] Transform[] waypoints;

    private int currentTargetIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Transform target = waypoints[currentTargetIndex];
        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        if (Vector2.Distance(transform.position, target.position) < 0.05f)
        {
            currentTargetIndex++;

            if (currentTargetIndex >= waypoints.Length)
            {
                currentTargetIndex = 0;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)//プレイヤーが乗ったとき一緒に動く
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
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
