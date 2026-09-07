using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (menuName = "Guns/Laser Sniper")]
public class LaserSniper : Gun
{
    Charge chargeCircles;
    public float charge = 0.01f;
    public BeamBullet beamPrefab;
    public float initialMaxWidth;
    public Transform muzzleFlash;
    public bool breakOutOfWhile = false;
    public AudioClip chargeSound;
    private AudioSource pSource;

    public override void Initialize()
    {
        p = PlayerController.instance;
        chargeCircles = Charge.instance;
        pSource = p.GetComponent<AudioSource>();
        pSource.clip = chargeSound;
        pSource.loop = false;
        for (int i = 0; i < 5; i++)
        {
            chargeCircles.circles[i].gameObject.SetActive(true);
            chargeCircles.circles[i].transform.localScale = Vector3.zero;
        } 
        isCharging = false;
        beamPrefab.maxWidth = maxWidth;
        charge = .01f;
        p.shouldStopCoroutine = false;
    }
    public override IEnumerator Shoot()
    {
        if (!isCharging)
        {
            p.canShoot = false;
            p.shouldReplaceActiveCoroutine = false;
            isCharging = true;
            p.isShooting = true;
            WaitForFixedUpdate wait = new WaitForFixedUpdate();
            pSource.Play();
            while (p.fireButton.buttonPressed && !breakOutOfWhile)
            {
                charge += Time.fixedDeltaTime;
                if (charge > chargeTime)
                    charge = chargeTime;
                int i = Mathf.FloorToInt(charge / chargeTime * 5);
                if (i < 5)
                {
                    if (i > 0)
                        chargeCircles.circles[i - 1].transform.localScale = Vector3.one;
                    chargeCircles.circles[i].transform.localScale = Vector3.one * ((charge / chargeTime * 5) - i);
                }
                yield return wait;
            }
            if (!breakOutOfWhile)
            {
                p.standardInput = false;
                p.canMove = false;
                p.shouldTurn = false;
                for (int i = 0; i < 5; i++)
                {
                    chargeCircles.circles[i].transform.localScale = Vector3.zero;
                }
                pSource.Stop();
                SoundManager.instance.soundSource.PlayOneShot(p.ship.primarySound);
                Instantiate(beamPrefab.muzzleFlash, p.firePoints[0].position, p.firePoints[0].rotation);
                BeamBullet beam = Instantiate(beamPrefab, p.firePoints[0].position, p.firePoints[0].rotation);
                BeamSetup(beam);
                bool isCrit;
                if (charge >= chargeTime)
                {
                    isCrit = Random.Range(0, 51) < p.critChance;
                }
                else
                {
                    isCrit = Random.Range(0, 101) < p.critChance;
                }
                if (isCrit)
                    beam.damage = p.damagePerBullet * charge / chargeTime * p.critMultiplier;
                else
                    beam.damage = p.damagePerBullet * charge / chargeTime;
                beam.isCrit = isCrit;
                beam.pushBack *= p.bulletPushBack * charge / chargeTime * (isCrit ? p.critMultiplier : 1);
                beam.charged = charge / chargeTime;
                p.justShot = true;

                p.rb.AddForce(-p.transform.up * charge / chargeTime * p.ship.damagePerBullet / 1.5f, ForceMode2D.Impulse);
                float lastCharge = charge;
                charge = 0.01f;
                p.shouldReplaceActiveCoroutine = true;
                isCharging = false;
                p.canShoot = true;
                yield return new WaitForSeconds(.25f * lastCharge / chargeTime);
            }
            else
            {
                breakOutOfWhile = false;
                isCharging = false;
                p.canShoot = true;
                p.shouldReplaceActiveCoroutine = true;
                for (int i = 0; i < 5; i++)
                {
                    chargeCircles.circles[i].transform.localScale = Vector3.zero;
                }
            }

            p.rb.velocity = Vector2.zero;
            p.standardInput = true;
            p.canMove = true;
            p.shouldTurn = true;
            p.isShooting = false;
        }
    }

    void BeamSetup(BeamBullet beam)
    {
        beam.canPierce = p.ship.canPierce;
        beam.canBounce = p.ship.canBounce;
        beam.canExplode = p.ship.canExplode;
    }
    public override void Reset()
    {
        maxWidth = initialMaxWidth;
    }

    public override void CancelShooting()
    {
        charge = 0.01f;
        breakOutOfWhile = true;
        isCharging = false;
    }
}
