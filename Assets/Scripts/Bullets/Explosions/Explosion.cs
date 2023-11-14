using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public float damage = 0;
    public bool hasPushBack = true;
    public bool isCrit;
    public AudioClip explosionSound;
    public AudioSource source;
    public bool isFinite = true;
    public float lifeTime;
    void Start()
    {
        if (damage > 0)
        {
            if (!isCrit)
                GetComponentInChildren<ExplosionDamage>().damage = damage;
            else
            {
                GetComponentInChildren<ExplosionDamage>().damage = damage * PlayerController.instance.critMultiplier * .8f;
                GetComponentInChildren<ExplosionDamage>().isCrit = true;
            }
            foreach (Transform child in GetComponentsInChildren<Transform>())
                child.gameObject.layer = gameObject.layer;
            GetComponentInChildren<ExplosionDamage>().hasPushBack = hasPushBack;
        }
        if (source)
        {
            source.clip = explosionSound;
            source.PlayOneShot(explosionSound);
        }
    }

    private void OnEnable()
    {
        if (isFinite)
            Destroy(gameObject, this.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length);
        else
            Destroy(gameObject, lifeTime);
    }


}
