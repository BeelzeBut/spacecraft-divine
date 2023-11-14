using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarkCircle : MonoBehaviour
{
    bool shouldGoBig = true;
    void Start()
    {
        StartCoroutine(GoBig());
    }

    // Update is called once per frame
    void Update()
    {
        if(shouldGoBig)
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(.7f, .7f, 0), 1.5f * Time.deltaTime);
        else
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(0, 0, 0), 5 * Time.deltaTime);

    }

    IEnumerator GoBig()
    {
        yield return new WaitForSeconds(2f);
        shouldGoBig = false;
    }
}
