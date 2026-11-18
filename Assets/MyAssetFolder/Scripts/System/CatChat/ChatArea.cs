using UnityEngine;

public class ChatArea : MonoBehaviour
{
    public string text;
    private CatChat catChat;

    [SerializeField] private bool willVanish = false;
    [SerializeField] private float vanishTime = 0f;

    // プレイヤーが現在このエリア内にいるかどうかを記録するフラグ
    private bool isPlayerInside = false;

    void Start()
    {
        catChat = FindObjectOfType<CatChat>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 大砲から発射されて物理演算が復活した際の2重カウントを防止
            if (!isPlayerInside)
            {
                isPlayerInside = true;
                catChat.ShowText(text);
                catChat.colliderCount++;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

            // プレイヤーの物理演算がオフになったことによる強制Exitかチェック
            if (player != null && rb != null && !rb.simulated)
            {
                // ★大砲に入った時は「生きてる」ので、Exit処理をキャンセルしてテキストを残す
                if (!player.IsDead)
                {
                    return;
                }

                // ★死亡した時は「死んでる」ので、そのまま下の処理に進んでテキストを消す
            }

            // 本当にエリアから出た、または「死亡した」場合のみ処理する
            if (isPlayerInside)
            {
                isPlayerInside = false;
                catChat.HideText();
                catChat.colliderCount--;

                if (catChat.colliderCount <= 0)
                {
                    catChat.colliderCount = 0;
                }

                if (willVanish == true)
                {
                    Destroy(gameObject, vanishTime);
                }
            }
        }
    }

    // 外部からテキストを受け取り、状況に応じて更新する
    public void UpdateAndShowText(string newText)
    {
        // ① テキストの変数自体は（外にいても中にいても）必ず上書きする
        text = newText;

        // ② プレイヤーが「エリア内にいる時」だけ、画面のUIも即座に更新する
        if (catChat != null && isPlayerInside)
        {
            catChat.HideText();
            catChat.ShowText(text);
        }
    }
}