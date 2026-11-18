using UnityEngine;

public class BGMManager : MonoBehaviour
{
    private bool isBGM = false;
    private AudioSource audioSource;
    private Collider2D childCollider;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // 子オブジェクトのCollider2Dを取得して保持しておく
        childCollider = GetComponentInChildren<Collider2D>();

        if (childCollider != null)
        {
            childCollider.enabled = true;
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        // プレイヤー(Playerタグ)が触れた時だけ処理する
        if (other.CompareTag("Player"))
        {
            // 1. BGMの切り替え処理
            if (!isBGM)
            {
                audioSource.Play();
                isBGM = true;
            }
            else
            {
                audioSource.Stop();
                isBGM = false;
            }

            // 2. 触れた子オブジェクトを使えなくする（非アクティブにして画面からも消す）
            if (childCollider != null)
            {
                childCollider.gameObject.SetActive(false);

                // ※もし見た目（画像など）は残したまま、当たり判定だけを消したい場合は
                // 上の行を消して、以下の行を使ってください。
                // childCollider.enabled = false; 
            }
        }
    }
}