using UnityEngine;

public class LeverDoorTrigger : MonoBehaviour
{
    [Header("Lever")]
    public HingeJoint leverHinge;

    [Header("Door")]
    public Transform doorTransform;
    public float doorOpenAngle = 120f;

    [Header("Trigger Logic")]
    public float openTriggerAngle = -5f;
    public float resetAngle = -2f;

    [Header("Audio")]
    public AudioClip doorOpenClip;
    public AudioClip doorCloseClip;
    public AudioSource leverAudioSource;

    private Quaternion doorClosedRot;
    private Quaternion doorOpenRot;

    private bool doorOpened = false;
    private bool leverLocked = false;

    void Start()
    {
        doorClosedRot = doorTransform.localRotation;
        doorOpenRot = Quaternion.Euler(
            doorClosedRot.eulerAngles + new Vector3(0, doorOpenAngle, 0)
        );
    }

    void Update()
    {
        float angle = leverHinge.angle;

        Debug.Log("Angle=" + angle.ToString("F2") +
                   " | doorOpened=" + doorOpened +
                   " | leverLocked=" + leverLocked);

        if (leverAudioSource != null)
        {
            if (Mathf.Abs(angle) > 2f && !leverAudioSource.isPlaying)
                leverAudioSource.Play();
            else if (Mathf.Abs(angle) <= 2f)
                leverAudioSource.Stop();
        }

        if (!leverLocked && angle < openTriggerAngle)
        {
            AudioSource doorAudio = doorTransform.GetComponent<AudioSource>();

            if (!doorOpened)
            {
                doorTransform.localRotation = doorOpenRot;
                if (doorAudio && doorOpenClip)
                {
                    doorAudio.clip = doorOpenClip;
                    doorAudio.Play();
                }
            }
            else
            {
                doorTransform.localRotation = doorClosedRot;
                if (doorAudio && doorCloseClip)
                {
                    doorAudio.clip = doorCloseClip;
                    doorAudio.Play();
                }
            }

            doorOpened = !doorOpened;
            leverLocked = true;
        }

        if (leverLocked && angle > resetAngle)
        {
            leverLocked = false;
        }
    }
}
