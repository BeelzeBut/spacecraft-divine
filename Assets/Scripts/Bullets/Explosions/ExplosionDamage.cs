using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionDamage : MonoBehaviour
{
    [HideInInspector]
    public Transform ignoredTarget;
    public float damage;
    public bool hasPushBack = true;
    public bool shouldMakeSound = true;
    public float pushBack;
    public bool isCrit;
    public AudioClip explosionSound;
    void Start()
    {
        if (shouldMakeSound)
        {
            Vector3 pos = Camera.main.WorldToViewportPoint(transform.position);
            if (!(pos.x < 0 || pos.x > 1 || pos.y < 0 || pos.y > 1))
                CameraShake.instance.StartShake(Mathf.Clamp(damage / 20f, .05f, .1f), .075f);
            SoundManager.instance.soundSource.PlayOneShot(explosionSound);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (PlayerController.instance.invincibility <= 0)
            {
                if (PlayerController.instance.canTakeDamage)
                {
                    Vector2 forceDir = (other.transform.position - transform.position).normalized;
                    PlayerController.instance.rb.AddForce(forceDir * damage, ForceMode2D.Impulse);
                }
            }
            PlayerController.instance.TakeDamage(damage);
        }

        if (other.CompareTag("Enemy") && other.transform != ignoredTarget)
        {
            Enemy enemy;
            if (other.GetComponent<Enemy>() != null)
            {
                enemy = other.GetComponent<Enemy>();
            }
            else
            {
                enemy = other.GetComponentInParent<Enemy>();
            }

            if (enemy.canTakeDamage)
            {
                enemy.TakeDamage(damage * (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f));
                if (isCrit)
                    Instantiate(GameManager.instance.criticalText, other.transform.position, Quaternion.identity);
                if (hasPushBack && enemy.canBePushedBack)
                {
                    Vector2 forceDir = (other.transform.position - transform.position).normalized;

                    if(pushBack == 0)
                        other.GetComponent<Rigidbody2D>().AddForce(forceDir * damage / 2f, ForceMode2D.Impulse);
                    else
                        other.GetComponent<Rigidbody2D>().AddForce(forceDir * pushBack, ForceMode2D.Impulse);
                }
            }
            else
            {
                Instantiate(GameManager.instance.immuneText, other.transform.position, Quaternion.identity);
            }

        }

    }
}
