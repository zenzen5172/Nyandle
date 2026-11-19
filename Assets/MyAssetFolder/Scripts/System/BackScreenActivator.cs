using UnityEngine;

public class BackScreenActivator : MonoBehaviour
{
    [SerializeField] private GameObject backScreen;

    void Start()
    {
        backScreen.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // プレイヤーが触れたら背景をオン
        if (other.CompareTag("Player"))
        {
            backScreen.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // ★ポイント★
            // プレイヤーの物理演算が停止している（＝大砲に入った状態）なら、エリアから出た扱いにはしない
            if (other.attachedRigidbody != null && !other.attachedRigidbody.simulated)
            {
                return; // ここで処理を中断し、背景を消さない
            }

            // 本当に歩いてエリアから出た場合のみ背景をオフ
            backScreen.gameObject.SetActive(false);
        }
    }
}