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

    [Header("エンディング演出設定(カメラ)")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float targetCameraSize = 15.0f;
    [SerializeField] private Vector3 panStartPosition = new Vector3(-30f, 0f, -10f);
    [SerializeField] private Vector3 panEndPosition = new Vector3(30f, 0f, -10f);
    [SerializeField] private float zoomOutDuration = 0.5f;
    [SerializeField] private float panDuration = 5.0f;
    [SerializeField] private float zoomInDuration = 0.5f;

    [Header("エンディング演出設定(タイミング・フェード)")]
    [SerializeField] private float resultTextDelay = 1.0f;
    [SerializeField] private float delayBeforeAscend = 1.0f;
    [SerializeField] private float delayBeforeFade = 2.0f;
    [SerializeField] private float fullFadeDuration = 4.0f;
    [SerializeField] private float spaceEnableOffset = 2.0f;
    [SerializeField] private float spaceTextDelay = 1.5f;

    [SerializeField] private Image fadeImage;
    [SerializeField] private string nextSceneName = "TitleScene";

    [Header("状態フラグ")]
    public bool isOpening = true;
    public bool canEnding = false;
    public bool isEnding = false;

    private bool movingSwitch = true;
    private bool hasStartedEndingSequence = false;
    private bool canAscend = false;

    private bool isCameraLocked = false;
    private Vector3 lockedCameraPosition;
    private float lockedCameraSize;

    [Header("サウンド設定")]
    [SerializeField] private AudioClip elevatorSound;
    [SerializeField] private float audioStartDelay = 0.5f; // ★乗ってから音が鳴るまでの一瞬の間
    [SerializeField] private float audioFadeDuration = 1.0f;
    public AudioSource audioSource;
    private Rigidbody2D elevatorRb;
    private float initialVolume = 1.0f;

    void Start()
    {
        elevatorRb = GetComponent<Rigidbody2D>();
        if (elevatorRb == null) elevatorRb = gameObject.AddComponent<Rigidbody2D>();
        elevatorRb.bodyType = RigidbodyType2D.Kinematic;

        transform.position = new Vector2(elevatorX, elevatorY);

        audioSource = GetComponent<AudioSource>();
        if (elevatorSound != null) audioSource.clip = elevatorSound;

        if (audioSource != null)
        {
            initialVolume = audioSource.volume;
            audioSource.Stop();
        }

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        if (mainCamera == null) mainCamera = Camera.main;
    }

    void FixedUpdate()
    {
        if (isOpening) OpeningElevator();
        else if (isEnding) EndingElevator();
    }

    void LateUpdate()
    {
        if (isCameraLocked && mainCamera != null)
        {
            mainCamera.transform.position = lockedCameraPosition;
            mainCamera.orthographicSize = lockedCameraSize;
        }
    }

    private void OpeningElevator()
    {
        MoveElevator(openingTargetPosition, () => { isOpening = false; canEnding = true; });
    }

    public void EndingElevator()
    {
        if (!hasStartedEndingSequence)
        {
            hasStartedEndingSequence = true;

            // ★追加：エンディングに入った瞬間（エレベーターに乗った直後）に、一瞬遅らせて音を鳴らす
            StartCoroutine(PlayEndingAudioImmediately());

            StartCoroutine(EndingSequence());
        }

        if (canAscend)
        {
            MoveElevator(endingTargetPosition, () => { isEnding = false; });
        }
    }

    // ★追加：エンディングに入った直後に一瞬遅れてエレベーター音を再生する
    private IEnumerator PlayEndingAudioImmediately()
    {
        yield return new WaitForSeconds(audioStartDelay);

        if (audioSource != null)
        {
            audioSource.volume = initialVolume;
            audioSource.time = 0f;
            audioSource.Play();
        }
    }

    private IEnumerator EndingSequence()
    {
        if (fadeImage != null) fadeImage.transform.SetAsFirstSibling();

        if (mainCamera != null)
        {
            mainCamera.transform.SetParent(null);
        }

        StartCoroutine(StartResultTextWithDelay());

        // 1. カメラスライド演出
        if (mainCamera != null)
        {
            float originalSize = mainCamera.orthographicSize;
            Vector3 originalPos = mainCamera.transform.position;

            float timer = 0f;
            while (timer < zoomOutDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, timer / zoomOutDuration);
                mainCamera.orthographicSize = Mathf.Lerp(originalSize, targetCameraSize, t);
                mainCamera.transform.position = Vector3.Lerp(originalPos, panStartPosition, t);
                yield return null;
            }
            timer = 0f;
            while (timer < panDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, timer / panDuration);
                mainCamera.transform.position = Vector3.Lerp(panStartPosition, panEndPosition, t);
                yield return null;
            }
            timer = 0f;
            while (timer < zoomInDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, timer / zoomInDuration);
                mainCamera.orthographicSize = Mathf.Lerp(targetCameraSize, originalSize, t);
                mainCamera.transform.position = Vector3.Lerp(panEndPosition, originalPos, t);
                yield return null;
            }

            lockedCameraPosition = mainCamera.transform.position;
            lockedCameraSize = mainCamera.orthographicSize;
            isCameraLocked = true;
        }

        yield return new WaitForSeconds(delayBeforeAscend);

        canAscend = true;

        yield return new WaitForSeconds(delayBeforeFade);

        if (fadeImage != null) fadeImage.gameObject.SetActive(true);

        float fadeTimer = 0f;
        bool canPressSpace = false;
        bool hasShownSpaceText = false;
        float timeSinceSpaceEnabled = 0f;

        while (true)
        {
            if (fadeTimer < fullFadeDuration)
            {
                fadeTimer += Time.deltaTime;
                float progress = fadeTimer / fullFadeDuration;

                if (fadeImage != null)
                {
                    Color c = fadeImage.color;
                    c.a = Mathf.Clamp01(progress);
                    fadeImage.color = c;
                }

                if (audioSource != null && audioSource.isPlaying && isEnding)
                {
                    audioSource.volume = Mathf.Lerp(initialVolume, 0f, progress);
                }
            }

            if (!canPressSpace && fadeTimer >= (fullFadeDuration - spaceEnableOffset))
            {
                canPressSpace = true;
            }

            if (canPressSpace && !hasShownSpaceText)
            {
                timeSinceSpaceEnabled += Time.deltaTime;
                if (timeSinceSpaceEnabled >= spaceTextDelay)
                {
                    hasShownSpaceText = true;
                    Result result = GetComponent<Result>();
                    if (result == null) result = FindObjectOfType<Result>();
                    if (result != null) result.ShowSpacePrompt();
                }
            }

            if (canPressSpace && Input.GetKeyDown(KeyCode.Space))
            {
                break;
            }

            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator StartResultTextWithDelay()
    {
        yield return new WaitForSeconds(resultTextDelay);

        Result result = GetComponent<Result>();
        if (result == null) result = FindObjectOfType<Result>();
        if (result != null) StartCoroutine(result.ShowResultUI());
    }

    private void MoveElevator(Vector2 target, System.Action onArrived)
    {
        if (movingSwitch)
        {
            movingSwitch = false;

            // ★変更：オープニング時のみ、移動開始から遅れて音を鳴らす（エンディング時はすでに鳴っているため重複再生を防ぐ）
            if (isOpening)
            {
                StartCoroutine(PlayElevatorAudioWithDelay());
            }
        }

        Vector2 nextPos = Vector2.MoveTowards(elevatorRb.position, target, moveSpeed * Time.fixedDeltaTime);
        elevatorRb.MovePosition(nextPos);

        if (Vector2.Distance(elevatorRb.position, target) < 0.01f)
        {
            elevatorRb.position = target;
            movingSwitch = true;

            StartCoroutine(FadeOutArrivalAudio());
            onArrived?.Invoke();
        }
    }

    private IEnumerator PlayElevatorAudioWithDelay()
    {
        yield return new WaitForSeconds(audioStartDelay);

        if (!movingSwitch && audioSource != null)
        {
            audioSource.volume = initialVolume;
            audioSource.time = 0f;
            audioSource.Play();
        }
    }

    private IEnumerator FadeOutArrivalAudio()
    {
        if (audioSource == null) yield break;

        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < audioFadeDuration)
        {
            timer += Time.deltaTime;
            if (audioSource != null)
            {
                audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / audioFadeDuration);
            }
            yield return null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}