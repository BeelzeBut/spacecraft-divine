using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArchBulletTest : Bullet
{
    public Transform bulletSprite;
    public Vector3 targetPos;
    public bool reachedCenter;
    private float bulletSpeed;
    public float initialBulletSpeed;
    public float timeToDestination = 1f;

    private void Start()
    {
        bulletSpeed = initialBulletSpeed;
        speed = (targetPos - transform.position).magnitude * (1 / timeToDestination) - .05f;
        StartCoroutine(ChangeBulletDirection());
    }
    void Update()
    {

        if (!reachedCenter)
        {
            bulletSprite.position += Vector3.up * bulletSpeed * Time.deltaTime * (1 / timeToDestination);
            bulletSpeed -= initialBulletSpeed * 2f * Time.deltaTime * (1 / timeToDestination);
        }
        else
        {
            bulletSprite.position -= Vector3.up * bulletSpeed * Time.deltaTime * (1 / timeToDestination);
            bulletSpeed += initialBulletSpeed * 2f * Time.deltaTime * (1 / timeToDestination) ;
        }

       /* if ((transform.position - targetPos).sqrMagnitude < .025f)
        {
            Explode();
            Destroy(gameObject);
        }*/
    }
    IEnumerator ChangeBulletDirection()
    {
        yield return new WaitForSeconds(timeToDestination / 2f);
        reachedCenter = true;
        yield return new WaitForSeconds(timeToDestination / 2f);
        explosion.GetComponent<Explosion>().damage = damage;
        if(gameObject.layer == 10)
            explosion.GetComponent<Explosion>().damage *= (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f);
        Explode();
        Destroy(gameObject);
    }
}
