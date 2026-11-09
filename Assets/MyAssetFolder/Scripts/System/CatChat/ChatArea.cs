using UnityEngine;

public class ChatArea : MonoBehaviour
{
    public string text;
    private CatChat catChat;

    [SerializeField] private bool willVanish = false;
    [SerializeField] private float vanishTime = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        catChat = FindObjectOfType<CatChat>();
    }

    // Update is called once per frame
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            catChat.ShowText(text);
            catChat.colliderCount++;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            catChat.HideText();
           catChat.colliderCount--;
            if (catChat.colliderCount <= 0)
            {
                catChat.colliderCount = 0; 
            }
        }

        if (willVanish == true)
        {
            Destroy(gameObject, vanishTime);
        }
    }

    public void UpdateAndShowText(string newText)
    {
        text = newText;
        if (catChat != null)
        {
            catChat.HideText();
            catChat.ShowText(text);

        }
    }

}

