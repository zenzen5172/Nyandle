using UnityEngine;

public class EndingSwitch : MonoBehaviour
{
    [Header("動かしたいエレベーター")]
    [SerializeField] private Elevator targetElevator; // Inspectorからドラッグ＆ドロップで割り当てる

    [Tooltip("触れた瞬間に横移動をピタッと止めるか（falseなら慣性で滑る）")]
    public bool stopHorizontalMovement = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        // ※Player.cs側で 変数名を canControlForever にした場合はこのままでOKです
        if (player == null || player.canControlForever) return;

        // 1. プレイヤーの操作をロック
        player.canControl = false;
        player.canControlForever = true;

        if (stopHorizontalMovement)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        // 2. エレベーターをエンディングへ向かわせる
        if (targetElevator != null)
        {
            targetElevator.isEnding = true;
        }

        // 3. このスイッチが二度と反応しないように、自身のコライダー（当たり判定）だけを消す
        GetComponent<Collider2D>().enabled = false;
    }
}