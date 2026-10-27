using UnityEngine;

public class ItemArrow : MonoBehaviour
{
    [SerializeField] public int addArrowAmount = 5; // 追加する矢の数
    [SerializeField] public AudioClip getSound;     // 取得時の効果音

    public ArrowManager arrowManager; // インスペクターから紐づけるか、Findで取得する必要があります

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 触れたオブジェクトのタグが "Player" の場合のみ処理を行う
        if (collision.gameObject.CompareTag("Player"))
        {
            CollectArrow();
        }
    }

    private void CollectArrow()
    {
        // 1. ArrowManagerの所持数を増やす（ArrowManager側に AddArrow メソッドがある想定）
        if (arrowManager != null)
        {
            arrowManager.AddArrow(addArrowAmount);
        }
        else
        {
            Debug.LogWarning("ArrowManagerが設定されていません！");
        }

        // 2. 取得時の効果音を再生する（アイテムが消えても鳴り終わるまで再生される方法）
        if (getSound != null)
        {
            // カメラの位置で音を鳴らすと2Dゲームで均等に聞こえます
            AudioSource.PlayClipAtPoint(getSound, Camera.main.transform.position);
        }

        // 3. 取得されたので、このアイテム（矢）を画面から消去する
        Destroy(gameObject);
    }
}