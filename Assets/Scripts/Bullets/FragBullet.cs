using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragBullet : ExplodeBullet
{
    public int numberOfSmallBullets;
    public Bullet smallBulletPrefab; // Bigger than 1 
    public float explosionAngle;
    public bool shouldDetonateOnImpact = true;
    public float smallBulletDamage;
    public AudioClip fragSound;
    void Update()
    {
        //transform.position += transform.right * Time.deltaTime * speed;
        if (explodeAtLocation)
        {
            if ((locationToExplode - transform.position).sqrMagnitude <= .04f)
            {
                Detonate(numberOfSmallBullets);
                Destroy(gameObject);
            }
        }
        else
        {
            if (explodeAfterTime)
            {
                explodeTimer -= Time.deltaTime;
                if (explodeTimer <= 0)
                {
                    Detonate(numberOfSmallBullets);
                    Destroy(gameObject);
                }
            }
        }
    }
    public void Detonate(int numberOfSmallBullets)
    {
        if (fragSound)
            SoundManager.instance.soundSource.PlayOneShot(fragSound);
        Vector3 deviation = new Vector3(0, 0, -explosionAngle / 2f);
        for (int i = 0; i < numberOfSmallBullets; i++)
        {
            Bullet smallBullet = Instantiate(smallBulletPrefab, transform.position, Quaternion.Euler(transform.rotation.eulerAngles + deviation));
            smallBullet.gameObject.layer = gameObject.layer;
            deviation += Vector3.forward * (explosionAngle / numberOfSmallBullets);
            smallBullet.whoShotIt = whoShotIt;
            if (smallBulletDamage > 0)
                smallBullet.damage = smallBulletDamage;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(shouldDetonateOnImpact)
            Detonate(numberOfSmallBullets);
        Destroy(gameObject);
    }
}
