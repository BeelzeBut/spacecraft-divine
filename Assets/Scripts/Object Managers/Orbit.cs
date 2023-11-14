using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orbit : MonoBehaviour
{

    void Update()
    {
        transform.Rotate(0, 0, 135 * Time.deltaTime, Space.World);
    }
}
