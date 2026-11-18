using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CheckPointLantern : MonoBehaviour
{
    private static CheckPointLantern currentActive;

    private SpriteRenderer sr;
    [SerializeField] private Color originalColor;
    [SerializeField] private Color lightColor;
    private AudioSource audioSource;
    public AudioClip checkSound;

    private Transform lightObject = null; 


    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        sr.color = originalColor;

        if (transform.childCount > 0)
        {
            lightObject = transform.GetChild(0);
        }

        lightObject.gameObject.SetActive(false);
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
                lightObject.gameObject.SetActive(true);
                sr.color = lightColor;
            }
        }
    } 

    public void ResetColor()
    {
        if (sr != null)
        {
            lightObject.gameObject.SetActive(false);
            sr.color = originalColor;
        }
    }
}
