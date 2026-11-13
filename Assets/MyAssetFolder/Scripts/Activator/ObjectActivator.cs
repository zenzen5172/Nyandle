using System.Threading.Tasks;
using UnityEngine;

public class ObjectActivator : MonoBehaviour
{
    [SerializeField] GameObject[] targetObjects;
    [SerializeField] int activateTime = 500;
    [SerializeField] private bool wasTouched = false;


    void Start()
    {

        foreach (GameObject target in targetObjects)
        {
            target.SetActive(false);
        }
    }

    private async Task OnTriggerEnter2D(Collider2D other)
    {
        await Task.Delay(activateTime);

        if (!wasTouched)
        {
            foreach (GameObject target in targetObjects)
            {
                target.SetActive(true);
            }
            wasTouched = true;
        }

        gameObject.SetActive(false);
        
    }
}