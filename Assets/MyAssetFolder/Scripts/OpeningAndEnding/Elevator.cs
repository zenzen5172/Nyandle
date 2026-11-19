using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class Elevator : MonoBehaviour
{
    [Header("初期位置・移動設定")]
    [SerializeField] private float elevatorX;
    [SerializeField] private float elevatorY;
    [SerializeField] private float moveSpeed = 2.0f;

    [Header("目標地点")]
    [SerializeField] private Vector2 openingTargetPosition;
    [SerializeField] private Vector2 endingTargetPosition;

    [Header("シーン遷移・フェード")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.5f; // 暗くなるのにかかる時間
    [SerializeField] private float endingFadeDelay = 2.0f; // ★追加：上昇開始から暗くなり始めるまでの待機時間
    [SerializeField] private string nextSceneName = "TitleScene";

    [Header("状態フラグ")]
    public bool isOpening = true;
    public bool canEnding = false;
    public bool isEnding = false;

    private bool movingSwitch = true;
    private bool hasStartedEndingSequence = false; // ★追加：フェード処理が重複して呼ばれないためのフラグ

    [Header("サウンド設定")]
    [SerializeField] private AudioClip elevatorSound;
    public AudioSource audioSource;

    private Rigidbody2D playerRb;
    private Player playerScript;

    private Rigidbody2D elevatorRb;

    void Start()
    {
        elevatorRb = GetComponent<Rigidbody2D>();
        if (elevatorRb == null)
        {
            elevatorRb = gameObject.AddComponent<Rigidbody2D>();
        }
        elevatorRb.bodyType = RigidbodyType2D.Kinematic;

        transform.position = new Vector2(elevatorX, elevatorY);
        audioSource = GetComponent<AudioSource>();

        if (elevatorSound != null)
        {
            audioSource.clip = elevatorSound;
        }
        audioSource.Stop();

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    void Update()
    {
        // 移動処理は書かない
    }

    void FixedUpdate()
    {
        if (isOpening)
        {
            OpeningElevator();
        }
        else if (isEnding)
        {
            EndingElevator();
        }
    }

    private void OpeningElevator()
    {
        MoveElevator(openingTargetPosition, () =>
        {
            isOpening = false;
            canEnding = true;
        });
    }

    public void EndingElevator()
    {
        // ★変更：Endingが始まった瞬間に、一度だけタイマー（フェード処理）をスタートさせる
        if (!hasStartedEndingSequence)
        {
            hasStartedEndingSequence = true;
            StartCoroutine(WaitAndFadeOutAndChangeScene());
        }

        // エレベーター自体の移動はそのまま続ける（目的地に着いたら移動を止めるだけ）
        MoveElevator(endingTargetPosition, () =>
        {
            isEnding = false;
        });
    }

    // ★変更：指定時間（endingFadeDelay）待ってからフェードアウトとシーン遷移を行う
    private IEnumerator WaitAndFadeOutAndChangeScene()
    {
        // 指定した秒数だけ待機
        yield return new WaitForSeconds(endingFadeDelay);

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color color = fadeImage.color;
            color = Color.black;
            color.a = 0f;
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                color.a = Mathf.Clamp01(timer / fadeDuration);
                fadeImage.color = color;
                yield return null;
            }
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void MoveElevator(Vector2 target, System.Action onArrived)
    {
        if (movingSwitch)
        {
            audioSource.time = 0f;
            audioSource.Play();
            movingSwitch = false;
        }

        Vector2 nextPos = Vector2.MoveTowards(
            elevatorRb.position,
            target,
            moveSpeed * Time.fixedDeltaTime
        );

        elevatorRb.MovePosition(nextPos);

        if (Vector2.Distance(elevatorRb.position, target) < 0.01f)
        {
            elevatorRb.position = target;
            audioSource.Stop();
            movingSwitch = true;
            onArrived?.Invoke();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
            playerRb = collision.GetComponent<Rigidbody2D>();
            playerScript = collision.GetComponent<Player>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!gameObject.activeInHierarchy) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);

            if (playerRb != null && collision.gameObject == playerRb.gameObject)
            {
                playerRb = null;
                playerScript = null;
            }
        }
    }
}