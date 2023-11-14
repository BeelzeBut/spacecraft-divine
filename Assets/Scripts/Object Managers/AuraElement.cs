using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuraElement : MonoBehaviour
{
    public float speed;
    public Vector3 direction;
    public float lifeTime;
    public SpriteRenderer spriteRenderer;
    public bool shouldChangeSide = true;

    private void Start()
    {
        if(shouldChangeSide)
            InvokeRepeating("ChangeSide", 0, 0.25f);
    }
    void Update()
    {
        if (lifeTime > 0)
        {
            lifeTime -= Time.deltaTime;
            transform.position += direction * Time.deltaTime;
            speed -= Time.deltaTime / (lifeTime + .2f);
            return;
        }

        StartCoroutine(Dissapear());
        lifeTime = 10f;
    }

    void ChangeSide()
    {
        spriteRenderer.flipX = !spriteRenderer.flipX;
    }

    IEnumerator Dissapear()
    {
        float elapsed = 0;
        Color desiredColor = new Color(1, 1, 1, 0);
        while(elapsed < 0.15f)
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, desiredColor, elapsed / 0.15f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
