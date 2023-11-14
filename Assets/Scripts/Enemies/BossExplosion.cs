using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossExplosion : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Explode());
    }

    IEnumerator Explode()
    {
        yield return new WaitForSeconds(.37f);
        CameraShake.instance.StartShake(.25f, .35f);
    }
}
