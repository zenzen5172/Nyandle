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
        // プレイヤーが乗っている時だけ、足場の移動量（差分）をプレイヤーにも加算する
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
            // プレイヤーのY座標が足場のY座標より上にあるか（上から乗ったか）を判定
            if (collision.transform.position.y > transform.position.y)
            {
                // 修正ポイント：SetParentは使わず、playerTransformに代入するだけ！
                playerTransform = collision.transform;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // プレイヤーが足場から離れたら null に戻して追従を止める
            if (playerTransform == collision.transform)
            {
                playerTransform = null;
            }
        }
    }
}