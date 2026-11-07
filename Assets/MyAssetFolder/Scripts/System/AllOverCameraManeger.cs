using UnityEngine;
using UnityEngine.InputSystem;

// Zキーでカメラを引きにして、ステージ全体を俯瞰する。
// 俯瞰中は十字キー／WASDで視点移動。もう一度Zで元のカメラに戻る。
// 俯瞰中はPlayerの操作を受け付けない（Player.IsInputLocked が参照する）。
public class AllOverCameraManeger : MonoBehaviour
{
    // 俯瞰中（戻りのアニメーション中も含む）かどうか（Playerなどが参照する）
    public static bool IsActive { get; private set; }

    [Header("対象のカメラ（未設定ならMain Camera）")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private StageCameraManager stageCamera; // 未設定ならカメラから自動取得

    [Header("俯瞰時のカメラの大きさ（Orthographic Size）")]
    [SerializeField] private float overviewSize = 20f;
    [SerializeField] private float zoomDuration = 0.4f; // 引き／戻りにかかる時間（秒）

    [Header("俯瞰中の視点移動速度")]
    [SerializeField] private float panSpeed = 25f;

    [Header("俯瞰中のゲーム進行を止める")]
    [SerializeField] private bool pauseWhileOverview = true;

    [Header("視点移動の範囲（カメラ中心が動ける範囲）")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 boundsMin = new Vector2(-10f, -40f);
    [SerializeField] private Vector2 boundsMax = new Vector2(150f, 10f);

    private float normalSize;
    private Vector3 normalPosition;
    private float zoomTimer;
    private float zoomFromSize;
    private float zoomToSize;
    private bool overviewOn; // 俯瞰中（視点移動できる状態）
    private bool returning;  // 元のカメラに戻るアニメーション中
    private float savedTimeScale = 1f;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (stageCamera == null && targetCamera != null)
            stageCamera = targetCamera.GetComponent<StageCameraManager>();
        IsActive = false;
        zoomTimer = zoomDuration; // 開始時はズームアニメーションを再生しない（サイズが0になるのを防ぐ）
    }

    void OnDisable()
    {
        if (IsActive) EndOverview(instant: true);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || targetCamera == null) return;

        if (kb.zKey.wasPressedThisFrame)
        {
            if (overviewOn) EndOverview(instant: false);
            else if (!returning) BeginOverview();
        }

        // 時間を止めても動くように unscaledDeltaTime を使う
        float dt = Time.unscaledDeltaTime;

        // ズームのアニメーション
        if (IsActive && zoomTimer < zoomDuration)
        {
            zoomTimer += dt;
            float t = zoomDuration > 0f ? Mathf.SmoothStep(0f, 1f, zoomTimer / zoomDuration) : 1f;
            targetCamera.orthographicSize = Mathf.Lerp(zoomFromSize, zoomToSize, t);

            if (returning)
            {
                // 俯瞰で動かした視点を元の位置へ戻す
                Vector3 p = Vector3.Lerp(targetCamera.transform.position, normalPosition, t);
                targetCamera.transform.position = p;
                if (zoomTimer >= zoomDuration) FinishReturn();
            }
        }

        if (!overviewOn) return;

        // 十字キー／WASDで視点移動
        Vector2 move = Vector2.zero;
        if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) move.x -= 1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) move.x += 1f;
        if (kb.downArrowKey.isPressed || kb.sKey.isPressed) move.y -= 1f;
        if (kb.upArrowKey.isPressed || kb.wKey.isPressed) move.y += 1f;

        if (move != Vector2.zero)
        {
            Vector3 pos = targetCamera.transform.position + (Vector3)(move.normalized * panSpeed * dt);
            if (useBounds)
            {
                pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
                pos.y = Mathf.Clamp(pos.y, boundsMin.y, boundsMax.y);
            }
            targetCamera.transform.position = pos;
        }
    }

    private void BeginOverview()
    {
        IsActive = true;
        overviewOn = true;
        normalSize = targetCamera.orthographicSize;
        normalPosition = targetCamera.transform.position;

        // 部屋単位の追従を止める
        if (stageCamera != null) stageCamera.enabled = false;

        if (pauseWhileOverview)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        StartZoom(overviewSize);
    }

    private void EndOverview(bool instant)
    {
        overviewOn = false;
        returning = true;
        StartZoom(normalSize);
        if (instant) FinishReturn();
    }

    private void FinishReturn()
    {
        IsActive = false;
        returning = false;
        zoomTimer = zoomDuration;
        targetCamera.orthographicSize = normalSize;
        targetCamera.transform.position = normalPosition;

        if (stageCamera != null) stageCamera.enabled = true;
        if (pauseWhileOverview) Time.timeScale = savedTimeScale;
    }

    private void StartZoom(float toSize)
    {
        zoomFromSize = targetCamera.orthographicSize;
        zoomToSize = toSize;
        zoomTimer = 0f;
    }

    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.cyan;
        Vector3 center = (boundsMin + boundsMax) / 2f;
        Vector3 size = boundsMax - boundsMin;
        Gizmos.DrawWireCube(center, size);
    }
}
