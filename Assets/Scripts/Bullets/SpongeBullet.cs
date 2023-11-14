using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpongeBullet : Bullet
{
    public Collider2D absorbtionCollider;
    private List<Bullet> bullets = new List<Bullet>();
    private bool hasStarted = false;
    public float damageAmp;
    public AudioClip sound;
    public float lifeTime = .75f;
    public Animator anim, rotationAnim;

    public IEnumerator StartAbsorbing(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        if (!hasStarted)
        {
            rotationAnim.SetTrigger("rotate");
            anim.SetTrigger("absorb");
            SoundManager.instance.soundSource.PlayOneShot(sound);
            hasStarted = true;
            speed = 0;
            StartCoroutine(DamageExplosion());
        }
    }

    IEnumerator DamageExplosion()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate(); 
        while(lifeTime > 0)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3, LayerMask.GetMask("Enemy Bullets"));
            foreach(Collider2D bulletCollider in hits)
            {
                AbsorbBullet(bulletCollider.GetComponent<Bullet>(), bulletCollider);
            }

            lifeTime -= Time.fixedDeltaTime; 
            yield return wait;
        }
        int n = bullets.Count;
        //anim.SetTrigger("stop");
        foreach (Bullet bullet in bullets)
            if(bullet)
                Destroy(bullet.gameObject);

        yield return new WaitForSeconds(.25f + lifeTime);
        damage *= (n / 10f);// (damageAmp + n / 15f);
        gameObject.layer = 10;
        transform.localScale *= 2;
        Debug.Log(n + " " + damage);
        explosion.GetComponent<Explosion>().damage = damage;
        Explode();
        Destroy(gameObject);
    }

    private void AbsorbBullet(Bullet bullet, Collider2D bulletCollider)
    {
        if (bullet.GetComponent<ElectricArchBullet>())
            return;
        if (Physics2D.Linecast(transform.position, bullet.transform.position, LayerMask.GetMask("Default") | LayerMask.GetMask("Obstacles")).collider == null)
        {
            bulletCollider.enabled = false;

            PatternBullet pattern = bullet.GetComponentInParent<PatternBullet>();
            if (pattern)
            {
                foreach (Bullet b in pattern.components)
                    b.transform.SetParent(null);
                pattern.components = null;
                pattern.helixBullets = null;
                pattern.isHelix = false;
                pattern.isRotative = false;
                pattern.CheckForComponents();
            }

            Vector2 lookDir = transform.position - bullet.transform.position;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
            bullet.speed = lookDir.magnitude / (lifeTime);
            bullet.shouldAccelerate = false;
            bullet.StopAllCoroutines();
         
            SlowingBullet slow = bullet.GetComponent<SlowingBullet>();
            if (slow)
            {
                slow.decreaseRateMax = 0;
                slow.decreaseRateMin = 0;
            }
            TargetedBullet target = bullet.GetComponent<TargetedBullet>();
            if(target)
            {
                target.target = null;
            }
            CurveBullet curve = bullet.GetComponent<CurveBullet>();
            if(curve)
            {
                curve.oscilationAmount = 0;
                curve.oscilationSpeed = 0;
            }
            bullets.Add(bullet);
            BoomerangBullet boomerang = bullet.GetComponent<BoomerangBullet>();
            if(boomerang)
            {
                boomerang.whoShotIt = null;
                boomerang.reachedTarget = true;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        speed = 0;
        rb.velocity = Vector2.zero;
        if(!hasStarted)
            StartCoroutine(StartAbsorbing(0));
    }
}
