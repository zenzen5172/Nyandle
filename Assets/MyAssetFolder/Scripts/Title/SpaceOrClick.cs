using UnityEngine;
using TMPro;

public class SpaceOrClick : MonoBehaviour
{
    private float opacity = 0f;
    [SerializeField] float opacityRange = 0.3f;
    [SerializeField] float changeSpeed = 1f;
    [SerializeField] float opacityPreset = 0.5f;

    public TMP_Text tmpText;


    void Start()
    {
        tmpText = GetComponent<TMP_Text>();
        opacity = 0;
    }

    // Update is called once per frame
    void Update()
    {
        tmpText.alpha = opacityRange * Mathf.Sin(Time.time * changeSpeed) + opacityPreset;
    }
}
