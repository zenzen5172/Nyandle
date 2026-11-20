using UnityEngine;

public class CaveTimer : MonoBehaviour
{
    public float timer = 0f;
    private bool isPlaying = false; // プレイ中かどうかを判定するフラグ
    public float caveTime = 0f;

    private Elevator el;

    void Start()
    {
        el = GetComponent<Elevator>();
        // 万が一別のオブジェクトにアタッチされていても探せるように安全対策
        if (el == null) el = FindObjectOfType<Elevator>();

        // シーン開始（Startが呼ばれた瞬間）と同時に計測をスタート
        timer = 0f;
        isPlaying = true;
    }

    void Update()
    {
        // エレベーターに乗ってエンディング演出が始まった瞬間にストップ
        if (el != null && el.isEnding && isPlaying)
        {
            StopCaveTimer();
        }

        // プレイ中であれば毎フレーム時間を加算
        if (isPlaying)
        {
            timer += Time.deltaTime;
        }
    }

    private void StopCaveTimer()
    {
        isPlaying = false;
        caveTime = timer; // 計測した時間をResultに渡す用の変数に保存
    }
}