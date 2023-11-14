using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningController : MonoBehaviour
{
    public LineRenderer line;
    void Start()
    {
        line = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (PlayerController.instance.fireButton.buttonPressed)
            line.enabled = true;
        else
            line.enabled = false;
    }
}
