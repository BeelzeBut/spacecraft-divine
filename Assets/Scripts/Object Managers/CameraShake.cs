using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake instance;

    private void Awake()
    {
        instance = this;
    }

    public void StartShake(float duration, float magnitude)
    {
        StartCoroutine(Shake(duration, magnitude));
    }
    public IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (Time.timeScale != 0)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                transform.localPosition = new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
            }
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}
