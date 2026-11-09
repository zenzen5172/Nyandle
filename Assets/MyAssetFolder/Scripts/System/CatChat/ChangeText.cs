using TMPro;
using UnityEngine;

public class ChangeText : MonoBehaviour
{
    [SerializeField] private string changeText;
    private ChatArea parentChatArea;
    // 一度だけ実行するための確認フラグ
    private bool isTriggered = false;
    private void Start()
    {
        // 1つ親のオブジェクトからコンポーネントを取得
        if (transform.parent != null)
        {
            parentChatArea = transform.parent.GetComponent<ChatArea>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 既に発動済みなら、これ以上処理しない
        if (isTriggered) return;

        if (other.CompareTag("Player"))
        {
            if (parentChatArea != null)
            {
                // テキストを更新して画面に表示
                parentChatArea.UpdateAndShowText(changeText);

                isTriggered = true;

                // 【重要】Destroyではなく SetActive(false) で無効化する。
                // これにより親の OnTriggerExit2D が正常に発火するようになります。
                gameObject.SetActive(false);
            }
        }
    }
}