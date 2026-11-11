using UnityEngine;

public class ChatArea : MonoBehaviour
{
    public string text;
    private CatChat catChat;

    [SerializeField] private bool willVanish = false;
    [SerializeField] private float vanishTime = 0f;

    // ★追加: プレイヤーが現在このエリア内にいるかどうかを記録するフラグ
    private bool isPlayerInside = false;

    void Start()
    {
        catChat = FindObjectOfType<CatChat>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true; // ★エリアに入ったことを記録

            catChat.ShowText(text);
            catChat.colliderCount++;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false; // ★エリアから出たことを記録

            catChat.HideText();
            catChat.colliderCount--;
            if (catChat.colliderCount <= 0)
            {
                catChat.colliderCount = 0;
            }
        }

        if (willVanish == true)
        {
            Destroy(gameObject, vanishTime);
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