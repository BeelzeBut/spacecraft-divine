using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArchedBullet : Bullet
{
    public Vector3 targetPosition;
    public float goDownTimer;
    public Transform impactAreaPrefab;
    private Transform impactArea;
    private bool freeToExplode;

    private void Start()
    {
        impactArea = Instantiate(impactAreaPrefab, targetPosition, Quaternion.identity);
        explosion.GetComponent<Explosion>().damage = damage;
        GameManager.instance.StartCoroutine(DestroyImpactArea(2f)); 
        if (gameObject.layer == 10)
            explosion.GetComponent<Explosion>().damage *= (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f);
    }
    void FixedUpdate()
    {
        goDownTimer -= Time.fixedDeltaTime;
        if(goDownTimer <= 0)
        {
            goDownTimer = 100f;
            transform.position = targetPosition + Vector3.up * 6f;
            transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z + 180);
            freeToExplode = true;
        }

        rb.MovePosition(rb.position + (Vector2)transform.right * speed * Time.fixedDeltaTime);

        if((targetPosition - transform.position).sqrMagnitude <= .1f && freeToExplode)
        {
            Explode();
            Destroy(impactArea.gameObject);
            Destroy(gameObject);
        }
    }

    public IEnumerator DestroyImpactArea(float time)
    {
        yield return new WaitForSeconds(time);
        if (impactArea)
            Destroy(impactArea.gameObject);
    }
}
