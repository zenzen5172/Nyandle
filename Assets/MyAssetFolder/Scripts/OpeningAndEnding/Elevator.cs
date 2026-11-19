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
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private string nextSceneName = "TitleScene";

    [Header("状態フラグ")]
    public bool isOpening = true;
    public bool canEnding = false;
    public bool isEnding = false;

    private bool movingSwitch = true;

    [Header("サウンド設定")]
    [SerializeField] private AudioClip elevatorSound;
    public AudioSource audioSource;

    private Rigidbody2D playerRb;
    private Player playerScript;

    void Start()
    {
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

    // ★ 変更：Update にあった移動処理をなくしました
    void Update()
    {
        // アニメーションや入力などがあればここに書きますが、移動処理は書きません
    }

    // ★ 変更：物理演算と同じタイミングでエレベーターを動かします
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
        MoveElevator(endingTargetPosition, () =>
        {
            isEnding = false;

            if (fadeImage != null)
            {
                StartCoroutine(FadeOutAndChangeScene());
            }
            else
            {
                SceneManager.LoadScene(nextSceneName);
            }
        });
    }

    private IEnumerator FadeOutAndChangeScene()
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

        // ★変更：FixedUpdate で動かすため、Time.deltaTime ではなく Time.fixedDeltaTime を使用します
        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.fixedDeltaTime
        );

        if (Vector2.Distance(transform.position, target) < 0.01f)
        {
            transform.position = target;
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