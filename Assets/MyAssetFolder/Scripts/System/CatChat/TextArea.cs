using UnityEngine;

public class ChatArea : MonoBehaviour
{
    public string text;
    private CatChat catChat;
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
    }

}
