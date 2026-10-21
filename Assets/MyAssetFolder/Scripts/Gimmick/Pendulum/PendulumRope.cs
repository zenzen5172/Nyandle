using UnityEngine;

public class PendulumRope : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField] private Transform woodBox;

    private LineRenderer lineRenderer;

    [SerializeField] private GameObject woodBoxObject;
    public DistanceJoint2D distanceJoint2D;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        distanceJoint2D = woodBoxObject.GetComponent<DistanceJoint2D>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (pivot != null && woodBox != null)
        {
            lineRenderer.SetPosition(0, pivot.position);
            lineRenderer.SetPosition(1, woodBox.position);
        }
    }

    // 🌟 プレイヤーが綱の当たり判定（トリガー）に触れたときの処理
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();

            // プレイヤーが燃えている（IsOnFire() が true）、またはスペースキーを押したら切断！
            if (player != null && (player.IsOnFire() || Input.GetKeyDown(KeyCode.Space)))
            {
                Cut();
            }
        }
    }

    private void Cut()
    {
        // 1. 箱を吊るしているジョイントを破壊して、箱を落下させる
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        // 2. 綱（このオブジェクト自身）をゲーム画面から完全に消す
  

        // ※ もし「後で復活させたい」場合は Destroy(gameObject) の代わりに
        // gameObject.SetActive(false); を使用してください。
    }
}
