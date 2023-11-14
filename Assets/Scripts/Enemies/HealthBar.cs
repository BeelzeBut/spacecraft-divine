using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    public SpriteRenderer green, healthBar;
    Vector3 scale = new Vector3(1.2f, .53f, 1);
    float decreaseVel;
    //[HideInInspector]
    public float maxHealth = 7f, currentHealth = 4f;
    public float showCooldown;
    [HideInInspector]
    public float showTimer;
    public float lerpTime = 10f;

    Vector3 position;

    private void Awake()
    {
        position = transform.localPosition;
    }
    void Start()
    {

        showTimer = 0;
        green.transform.localScale = scale;
        healthBar.color = new Color(1, 1, 1, 0);
        green.color = new Color(1, 1, 1, 0);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = transform.parent.transform.position + position;
        transform.rotation = Quaternion.identity;
        green.transform.localScale = new Vector3((currentHealth / maxHealth), 1, 1);
        if (showTimer > 0)
        {
            showTimer -= Time.deltaTime;
            if (showTimer <= 0)
            {
                StartCoroutine(LerpFunction(new Color(1, 1, 1, 0), 1f)); ;
            }
        } 

    }

    IEnumerator LerpFunction(Color endValue, float duration)
    {
        float time = 0;
        Color startValue = Color.white;

        while (time < duration)
        {
            healthBar.color = Color.Lerp(startValue, endValue, time / duration);
            green.color = Color.Lerp(startValue, endValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        healthBar.color = endValue;
        green.color = endValue;
    }

    public void ShowHealthBar()
    {
        StopAllCoroutines();
        healthBar.color = Color.white;
        green.color = Color.white;
        showTimer = showCooldown;
    }

}
