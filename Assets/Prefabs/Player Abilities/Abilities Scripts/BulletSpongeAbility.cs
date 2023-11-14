using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName = ("Abilities/Sponge Bullet Ability"))]
public class BulletSpongeAbility : Ability
{
    public float damageAmplification = 1.4f;
    private float damageAmp;
    public float explosionDamage;
    private float damage;
    public SpongeBullet bulletPrefab;
    public AudioClip sound;

    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        cooldown = baseCooldown - .5f * level + .5f;
        damageAmp = damageAmplification + .1f * level - .1f;
        damage = explosionDamage + .75f * level - .75f;
    }

    public override void TriggerAbility()
    {
        SoundManager.instance.soundSource.PlayOneShot(sound);
        Instantiate(bulletPrefab.muzzleFlash, p.transform.position + p.transform.up * .15f, Quaternion.Euler(p.transform.rotation.eulerAngles + Vector3.forward * 90));
        SpongeBullet bullet = Instantiate(bulletPrefab, p.transform.position + p.transform.up * .15f, Quaternion.Euler(p.transform.rotation.eulerAngles + Vector3.forward * 90));
        bullet.damageAmp = damageAmp;
        bullet.damage = damage;
        bullet.whoShotIt = p.transform;
        bullet.StartCoroutine(bullet.StartAbsorbing(.25f));
    }

    public override void ToActivateUpdate()
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateAbility()
    {
        throw new System.NotImplementedException();
    }

    public override void CancelUpdateAbility()
    {
        throw new System.NotImplementedException();
    }
}
