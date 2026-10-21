using UnityEngine;

public class Activator : MonoBehaviour
{
    public GameObject textSystem;
    void Start()
    {
        if (textSystem != null)
        {
            textSystem.SetActive(true);
        }
    }

    
}
