using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DoorWithKeySocket : MonoBehaviour
{
    public GameObject key;
    public Transform doorTransform;
    public float doorOpenAngle = 90f;
    public float speed = 3f;

    private Quaternion closedRot;
    private Quaternion openRot;
    private bool isOpening = false;

    void Start()
    {
        closedRot = doorTransform.localRotation;
        openRot = Quaternion.Euler(closedRot.eulerAngles + new Vector3(0, doorOpenAngle, 0));
    }

    void Update()
    {
        if (isOpening)
        {
            key.SetActive(false); // move this inside the if block
            doorTransform.localRotation = Quaternion.Lerp(
                doorTransform.localRotation,
                openRot,
                Time.deltaTime * speed
            );
        }
    }

    public void OnKeyInserted(SelectEnterEventArgs args)
    {
        if (args.interactableObject.transform.CompareTag("CorrectKey"))
        {
            Debug.Log("Correct key inserted. Door opening!");
            isOpening = true;
            Physics.autoSimulation = false;
        }
        else
        {
            Debug.Log("Wrong object inserted!");
            Physics.autoSimulation = false;
        }
    }
}