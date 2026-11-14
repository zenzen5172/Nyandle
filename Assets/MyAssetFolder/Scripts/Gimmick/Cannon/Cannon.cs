using System.Collections;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    [SerializeField] Transform cannonPoint; //発射口
    [SerializeField] Vector2 cannonDirection = new Vector2(1, 1);

    [Header("大砲のパワー")]
    [SerializeField] float strongPower = 20f; // 導火線ありの強いパワー
    [SerializeField] float weakPower = 5f;    // 導火線なしの弱いパワー

    [Header("強発射後の操作不能時間（秒）")]
    [SerializeField] float stunDuration = 1.0f;

    [Header("連携する導火線")]
    [SerializeField] CannonFuse fuse; // 導火線

    [Header("音声")]
    public AudioClip shotSound;
    private AudioSource audioSource;

    private bool isCannon = false;
    private bool jumpPressed = false; // 🌟入力を確実に検知するためのフラグ

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // 🌟コルーチン内での入力すり抜けを防ぐため、Update側で入力を監視する
        if (isCannon)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                jumpPressed = true;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // プレイヤーかつ、現在大砲が使用中でなければ
        if (collision.CompareTag("Player") && !isCannon)
        {
            StartCoroutine(CannonSequence(collision.gameObject));
        }
    }

    private IEnumerator CannonSequence(GameObject playerObj)
    {
        isCannon = true;
        jumpPressed = false; // 入力フラグをリセット

        Player player = playerObj.GetComponent<Player>();
        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        SpriteRenderer sr = playerObj.GetComponentInChildren<SpriteRenderer>();

        // プレイヤーの制御をオフにして大砲の中に隠す
        player.canControl = false;
        sr.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        playerObj.transform.position = transform.position;

        // 大砲に入った時点で導火線に火がついているかチェック
        bool isPowered = (fuse != null && fuse.isLit);

        if (isPowered)
        {
            // 導火線に火がついている場合：燃え尽きるまで自動待機
            while (fuse != null && fuse.isLit)
            {
                yield return null;
            }

            if (fuse != null)
            {
                fuse.CannonEntered();
            }
        }
        else
        {
            // 導火線に火がついていない場合：手動待機
            // 🌟Updateで取得したフラグを使って確実にジャンプ入力を待つ
            while (!jumpPressed)
            {
                yield return null;
            }
        }

        // --- 以下、発射処理 ---
        playerObj.transform.position = cannonPoint.position;
        sr.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;

        player.isLaunched = true;

        if (isPowered)
        {
            // 強発射
            rb.linearVelocity = cannonDirection * strongPower;
            player.canControl = false;

            if (shotSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(shotSound);
            }

            StartCoroutine(EnablePlayerControl(player, stunDuration));
        }
        else
        {
            // 弱発射
            rb.linearVelocity = cannonDirection * weakPower;

            // 🌟弱発射でも一瞬(0.15秒)だけ操作不可にし、Playerスクリプトによる速度の上書きを防ぐ
            player.canControl = false;
            StartCoroutine(EnablePlayerControl(player, 0.15f));
        }

        // 🌟発射後、すぐには大砲に再突入できないようにクールタイムを1秒確保する
        yield return new WaitForSeconds(1.0f);
        isCannon = false;
    }

    private IEnumerator EnablePlayerControl(Player player, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (player != null && !player.IsDead)
        {
            player.canControl = true;
        }
    }
}