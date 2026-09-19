using System.Collections;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    [SerializeField] Transform cannonPoint; //発射口
    [SerializeField] Vector2 cannonDirection = new Vector2(1, 1);

    [Header("大砲のパワー")]
    [SerializeField] float strongPower = 20f; // 導火線ありの強いパワー
    [SerializeField] float weakPower = 5f;    // 導火線なしの弱いパワー

    // 🌟【追加】強発射時にプレイヤーが操作できない時間
    [Header("強発射後の操作不能時間（秒）")]
    [SerializeField] float stunDuration = 1.0f;

    [Header("連携する導火線")]
    [SerializeField] CannonFuse fuse; // 導火線

    private bool isCannon = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isCannon)
        {
            StartCoroutine(CannonSequence(collision.gameObject));
        }
    }

    private IEnumerator CannonSequence(GameObject playerObj)
    {
        isCannon = true;

        Player player = playerObj.GetComponent<Player>();
        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        SpriteRenderer sr = playerObj.GetComponent<SpriteRenderer>();

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
            while (!Input.GetKeyDown(KeyCode.Space))
            {
                yield return null;
            }
        }

        // --- 以下、発射処理 ---
        playerObj.transform.position = cannonPoint.position;
        sr.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        player.isLaunched = true;

        // 🌟【変更】発射時のパワーと操作可能タイミングの分岐
        if (isPowered)
        {
            // 強発射
            rb.linearVelocity = cannonDirection.normalized * strongPower;
            player.canControl = false; // 🌟一旦操作不可のままにする
            StartCoroutine(EnablePlayerControl(player, stunDuration)); // 🌟指定秒数後に操作可能にする
        }
        else
        {
            // 弱発射
            rb.linearVelocity = cannonDirection.normalized * weakPower;
            player.canControl = true; // 🌟弱発射はすぐに操作可能
        }

        yield return new WaitForSeconds(0.5f);
        isCannon = false;
    }

    // 🌟【追加】指定した秒数だけ待ってから、プレイヤーの操作制限を解除する処理
    private IEnumerator EnablePlayerControl(Player player, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (player != null)
        {
            player.canControl = true;
        }
    }
}