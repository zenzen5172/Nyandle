using UnityEngine;

public class CheckPointLantern : MonoBehaviour
{
    private static CheckPointLantern currentActive;

    private SpriteRenderer sr;
    private Color originalColor;
    private AudioSource audioSource;
    public AudioClip checkSound;


    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        sr.color = Color.gray;
        originalColor = sr.color;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                if (currentActive == this) return; //触れたランタンが現在のチェックポイントなら何もしない
                

                player.respawnPoint = transform.position;
                if (currentActive != null)
                {
                    currentActive.ResetColor();
                }
                if (checkSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(checkSound);
                }
                currentActive = this;
                sr.color = Color.yellowGreen;
            }
        }
    } 

    public void ResetColor()
    {
        if (sr != null)
        {
            sr.color = originalColor;
        }
    }
}
