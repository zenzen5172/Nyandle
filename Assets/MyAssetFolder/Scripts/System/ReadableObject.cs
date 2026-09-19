using UnityEngine;

public class ReadableObject : MonoBehaviour
{
    [Header("表示するテキストのUI（Canvas内のパネルなど）")]
    [SerializeField] private GameObject dialogUI;

    private bool isPlayerInArea = false; // プレイヤーが近くにいるか
    private bool isReading = false;      // 現在テキストを開いているか

    void Start()
    {
        // ゲーム開始時はテキストUIを非表示にしておく
        if (dialogUI != null)
        {
            dialogUI.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} に Dialog UI が設定されていません！");
        }
    }

    void Update()
    {
        // プレイヤーが範囲内にいて、かつスペースキーが押された瞬間
        if (isPlayerInArea && Input.GetKeyDown(KeyCode.Space))
        {
            // 読書状態を反転させる（開いていれば閉じ、閉じていれば開く）
            isReading = !isReading;

            if (dialogUI != null)
            {
                dialogUI.SetActive(isReading);
            }
        }
    }

    // プレイヤーが読める範囲に入ったとき
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInArea = true;
        }
    }

    // プレイヤーが読める範囲から離れたとき
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInArea = false;
            isReading = false; // 読書状態をリセット

            // 離れたら強制的にテキストUIを閉じる
            if (dialogUI != null)
            {
                dialogUI.SetActive(false);
            }
        }
    }
}