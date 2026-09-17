using UnityEngine;

public class CuttableRope : MonoBehaviour
{
    [Header("箱を吊るしているジョイント（箱についているHingeJoint2Dなど）")]
    [SerializeField] private Joint2D boxJoint;

    // 🌟 プレイヤーが綱の当たり判定（トリガー）に触れたときの処理
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();

            // プレイヤーが燃えている（IsOnFire() が true）なら切断！
            if (player != null && player.IsOnFire())
            {
                Cut();
            }
        }
    }

    private void Cut()
    {
        // 1. 箱を吊るしているジョイントを破壊して、箱を落下させる
        if (boxJoint != null)
        {
            Destroy(boxJoint);
        }

        // 2. 綱（このオブジェクト自身）をゲーム画面から完全に消す
        Destroy(gameObject);

        // ※ もし「後で復活させたい」場合は Destroy(gameObject) の代わりに
        // gameObject.SetActive(false); を使用してください。
    }
}