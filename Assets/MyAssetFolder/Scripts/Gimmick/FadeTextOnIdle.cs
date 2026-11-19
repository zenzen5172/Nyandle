using UnityEngine;
using TMPro;

public class FadeTextOnIdle : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("同時にフェードさせたいTMPを複数登録できます")]
    public TextMeshProUGUI[] targetTexts;

    public float timeToWait = 2.0f;
    public float movementThreshold = 0.005f;

    [Tooltip("フェードの速さ（1なら1秒で完了、2なら0.5秒で完了）")]
    public float fadeSpeed = 2.0f;

    private Vector3 lastPosition;
    private float idleTimer = 0f;
    private float targetAlpha = 0f;

    void Start()
    {
        if (targetTexts != null)
        {
            // 登録されたすべてのTMPを初期化
            foreach (TextMeshProUGUI text in targetTexts)
            {
                if (text != null)
                {
                    text.gameObject.SetActive(true);
                    Color c = text.color;
                    c.a = 0f;
                    text.color = c;
                }
            }
        }

        lastPosition = transform.position;
    }

    void Update()
    {
        // 1. 移動判定とタイマー処理
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        if (distanceMoved > movementThreshold)
        {
            idleTimer = 0f;
            targetAlpha = 0f;
        }
        else
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= timeToWait)
            {
                targetAlpha = 1f;
            }
        }

        lastPosition = transform.position;

        // 2. 登録されたすべてのTMPの透明度を同時に変化させる
        if (targetTexts != null)
        {
            foreach (TextMeshProUGUI text in targetTexts)
            {
                if (text != null)
                {
                    Color c = text.color;
                    c.a = Mathf.MoveTowards(c.a, targetAlpha, fadeSpeed * Time.deltaTime);
                    text.color = c;
                }
            }
        }
    }
}