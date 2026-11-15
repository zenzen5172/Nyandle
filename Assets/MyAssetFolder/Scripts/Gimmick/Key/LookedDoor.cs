using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    public AudioClip unLockedSound;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    // 2D用の衝突判定
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerInventory inventory = collision.gameObject.GetComponent<PlayerInventory>();

            // プレイヤーが鍵を持っているかチェック
            if (inventory != null && inventory.hasKey)
            {
                inventory.hasKey = false; // 鍵を消費

                if (inventory.currentKey != null)
                {
                    Destroy(inventory.currentKey); // 鍵を消す
                }
                if (unLockedSound != null && audioSource != null)
                audioSource.PlayOneShot(unLockedSound);
                Destroy(gameObject); // 扉自体を消す
            }
        }
    }
}