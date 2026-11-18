using UnityEngine;

public class Blur : MonoBehaviour
{
    private SpriteRenderer sr;
    private float opacity;
    [SerializeField] float blurRatio = 0.3f; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        opacity = 1f;
        sr = GetComponent<SpriteRenderer>();

        SetOpacity(1f);
    }

    // Update is called once per frame
    void Update()
    {
        if (opacity >= 0)
        {
            opacity = opacity - Time.deltaTime * blurRatio;
            SetOpacity(opacity);
        }
        else
        {
            Destroy(gameObject);
        }
          
    }

    private void SetOpacity(float alpha)
    {
        Color currentColor = sr.color;
        currentColor.a = alpha;
        sr.color = currentColor;
    }

}
