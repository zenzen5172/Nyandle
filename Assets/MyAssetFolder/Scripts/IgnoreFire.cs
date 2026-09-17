using UnityEngine;

public class IgnoreFire : MonoBehaviour
{
    public GameObject fire;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (fire != null)
        {
            fire.SetActive(false);
        }
    }

    // Update is called once per frame
    // Update is called once per frame
   void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (fire != null)
            {
                fire.SetActive(true);
            }
        }
    }
}
