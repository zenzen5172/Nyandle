using UnityEngine;

public class BlackScreenActivator : MonoBehaviour
{
    public GameObject blackScreen;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (blackScreen!= null)
        {
            blackScreen.SetActive(true);
        }
    }


}
