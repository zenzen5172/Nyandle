using System.Collections;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    private bool isBGM = false;
    private AudioSource audioSource;
    private Collider2D childCollider;

    // フェードアウトにかかる時間（秒）
    [SerializeField] private float fadeDuration = 1.0f;

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
                // Stop()の代わりにコルーチンを呼び出してフェードアウトを開始する
                StartCoroutine(FadeOutBGM());
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

    // フェードアウトを実行するコルーチン
    private IEnumerator FadeOutBGM()
    {
        // 現在の音量を保存（通常は1.0などの設定値）
        float startVolume = audioSource.volume;
        float time = 0f;

        // 指定した時間（fadeDuration）をかけて音量を0まで下げる
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, time / fadeDuration);
            yield return null; // 次のフレームまで待機
        }

        // 完全に音が消えたら再生を停止
        audioSource.Stop();

        // 次に audioSource.Play() が呼ばれた時に音が鳴るよう、元の音量に戻しておく
        audioSource.volume = startVolume;
    }
}