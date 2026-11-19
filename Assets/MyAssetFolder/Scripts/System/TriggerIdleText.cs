using UnityEngine;
using TMPro;

public class TriggerIdleText : MonoBehaviour
{
    [Header("監視対象")]
    [Tooltip("プレイヤーを登録してください")]
    public Transform targetPlayer;

    [Header("この地点で表示・フェードさせるテキスト")]
    public TextMeshProUGUI[] targetTexts;

    [Header("フェード・待機設定")]
    public float timeToWait = 2.0f;
    public float fadeInSpeed = 2.0f;
    public float fadeOutSpeed = 6.0f;
    public float movementThreshold = 0.001f;

    [Header("エリアを出た後の挙動")]
    public bool keepActiveAfterLeave = true;

    private bool isActive = false;
    private Vector3 lastPosition;
    private float idleTimer = 0f;
    private float targetAlpha = 0f;

    private Rigidbody2D playerRb;
    private CanvasGroup[] canvasGroups;

    void Start()
    {
        if (targetPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) targetPlayer = playerObj.transform;
        }

        if (targetPlayer != null)
        {
            lastPosition = targetPlayer.position;
            playerRb = targetPlayer.GetComponent<Rigidbody2D>();
        }

        if (targetTexts != null)
        {
            canvasGroups = new CanvasGroup[targetTexts.Length];

            for (int i = 0; i < targetTexts.Length; i++)
            {
                if (targetTexts[i] != null)
                {
                    // 文字本体の色は不透明(1)にしておく
                    Color c = targetTexts[i].color;
                    c.a = 1f;
                    targetTexts[i].color = c;

                    CanvasGroup cg = targetTexts[i].GetComponent<CanvasGroup>();
                    if (cg == null)
                    {
                        cg = targetTexts[i].gameObject.AddComponent<CanvasGroup>();
                    }

                    cg.alpha = 0f;
                    canvasGroups[i] = cg;

                    // ★変更：最初は完全に非表示にしておく
                    targetTexts[i].gameObject.SetActive(false);
                }
            }
        }
    }

    void Update()
    {
        if (targetPlayer == null) return;

        if (!isActive)
        {
            targetAlpha = 0f;
        }
        else
        {
            bool isMoving = false;

            if (playerRb != null)
            {
                isMoving = playerRb.linearVelocity.sqrMagnitude > 0.01f;
            }
            else
            {
                float distanceMoved = Vector3.Distance(targetPlayer.position, lastPosition);
                isMoving = distanceMoved > movementThreshold;
            }

            if (isMoving)
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
        }

        lastPosition = targetPlayer.position;

        if (canvasGroups != null)
        {
            float currentSpeed = (targetAlpha == 1f) ? fadeInSpeed : fadeOutSpeed;

            foreach (CanvasGroup cg in canvasGroups)
            {
                if (cg != null)
                {
                    // ★追加：表示され始める瞬間、オブジェクトをオンにする
                    if (targetAlpha > 0f && !cg.gameObject.activeSelf)
                    {
                        cg.gameObject.SetActive(true);
                    }

                    cg.alpha = Mathf.MoveTowards(cg.alpha, targetAlpha, currentSpeed * Time.deltaTime);

                    // ★追加：完全に透明になったら、オブジェクト自体をオフにする（確実に消す）
                    if (cg.alpha <= 0f && targetAlpha == 0f)
                    {
                        cg.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isActive = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!keepActiveAfterLeave)
            {
                isActive = false;
            }
        }
    }
}