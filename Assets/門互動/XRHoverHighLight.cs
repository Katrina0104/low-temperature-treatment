using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

public class XRHoverHighLight : MonoBehaviour
{
    public GameObject outlineObject;

    public void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (outlineObject != null)
            outlineObject.SetActive(true);
    }

    public void OnHoverExited(HoverExitEventArgs args)
    {
        if (outlineObject != null)
            outlineObject.SetActive(false);
    }
}
