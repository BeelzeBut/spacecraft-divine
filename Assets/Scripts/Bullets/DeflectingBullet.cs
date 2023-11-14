using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeflectingBullet : MonoBehaviour
{
    public Rigidbody2D rb;
    public float speed;
    public float lifeTime = .5f;
    public SpriteRenderer sprite;
    public float damageMultiplier;
    public TrailRenderer trail;

    private void Start()
    {
        StartCoroutine(Coloring());
        speed += PlayerController.instance.curMoveSpeed * PlayerController.instance.moveDirection.magnitude;
        sprite.GetComponent<Animator>().SetTrigger("rotate");
    }

    public void FixedUpdate()
    {
        rb.MovePosition(rb.position + (Vector2)transform.right * speed * Time.fixedDeltaTime);
        Collider2D[] hits = Physics2D.OverlapAreaAll(transform.InverseTransformPoint(new Vector3(-.1f, -.2f)), transform.InverseTransformPoint(new Vector3(.1f, .2f)), LayerMask.GetMask("Enemy Bullets"));
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].GetComponent<Bullet>().canBeDeflected)
                DeflectBullet(hits[i].transform);
        }
    }
    IEnumerator Coloring()
    {
        float elapsed = 0;
        sprite.color = new Color(1, 1, 1, 0);
        Color color = Color.white;
        color.a = 0;
        while(elapsed < lifeTime / 6f)
        {
            color.a += Time.deltaTime * (1 / (lifeTime / 6f));
            sprite.color = Color.Lerp(sprite.color, color, elapsed / (lifeTime / 6f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(lifeTime * .58f);
        elapsed = 0;
        while(elapsed < lifeTime * .25f)
        {
            color.a -= Time.deltaTime * (1 / (lifeTime * .25f));
            sprite.color = Color.Lerp(sprite.color, color, elapsed / (lifeTime * .25f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Bullet>() && other.GetComponent<Bullet>().canBeDeflected)
            DeflectBullet(other.transform);
    }
    public void DeflectBullet(Transform bulletTransform)
    {
        Bullet bullet = bulletTransform.GetComponent<Bullet>();
        bullet.transform.SetParent(null);
        if (bullet.speed == 0)
            bullet.speed = bullet.latentSpeed;
        if (bullet)
        {
            if (!bullet.gameObject.GetComponent<ExplosionBullet>())
            {
                bullet.gameObject.layer = 10;
                Transform target = bullet.whoShotIt;
                if (target)
                {
                    if (bullet.gameObject.GetComponent<TargetedBullet>())
                    {
                        TargetedBullet tBullet = bullet.GetComponent<TargetedBullet>();
                        tBullet.target = tBullet.whoShotIt;
                    }
                    Vector2 lookDir = target.position - bullet.transform.position;
                    float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                    bullet.transform.rotation = Quaternion.Euler(0, 0, angle);//bullet.transform.rotation.eulerAngles.z + 180);
                }
                else
                {
                    bullet.transform.rotation = Quaternion.Euler(0, 0, bullet.transform.rotation.eulerAngles.z + 180);
                }

                if (bullet.gameObject.GetComponent<StickyBullet>())
                {
                    bullet.gameObject.GetComponent<StickyBullet>().isEnemy = false;
                }
                if (bullet.GetComponent<BoomerangBullet>())
                {
                    if(target)
                        bullet.GetComponent<BoomerangBullet>().targetPos = target.position;
                    bullet.GetComponent<BoomerangBullet>().reachedTarget = false;
                }

            }
            else
            {
                ExplosionBullet expBullet = bullet.gameObject.GetComponent<ExplosionBullet>();
                Transform target = expBullet.whoShotIt;
                expBullet.gameObject.layer = 10;

                if (target)
                {

                    Vector2 lookDir = target.position - expBullet.transform.position;
                    float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                    expBullet.transform.rotation = Quaternion.Euler(0, 0, angle);//bullet.transform.rotation.eulerAngles.z + 180);
                    expBullet.locationToExplode = target.position;
                }
                else
                {
                    expBullet.transform.rotation = Quaternion.Euler(0, 0, bullet.transform.rotation.eulerAngles.z + 180);
                    expBullet.locationToExplode = expBullet.transform.position + expBullet.transform.right;
                }

            }
            bullet.damage *= damageMultiplier;
            Destroy(bullet.gameObject.GetComponent<TrailRenderer>());
            Instantiate(trail, bullet.transform.position, bullet.transform.rotation, bullet.transform).time /= (.5f * bullet.speed);
        }
    }
}
