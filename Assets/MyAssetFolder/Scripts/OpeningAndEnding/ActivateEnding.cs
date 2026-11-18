using UnityEngine;

public class ActivateEnding : MonoBehaviour
{
    private Elevator elevator;
    private EndingSwitch endingSwitch;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        elevator = GetComponent<Elevator>();
        endingSwitch = GetComponent<EndingSwitch>();

        endingSwitch.gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider other)
    {
        elevator.canEnding = true;
        endingSwitch.gameObject.SetActive(true);

        Destroy(gameObject, 2);
    }
}
