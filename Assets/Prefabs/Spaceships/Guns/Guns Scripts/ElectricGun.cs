using DigitalRuby.LightningBolt;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName =("Guns/Electric Gun"))]
public class ElectricGun : Gun
{
    public LightningBoltScript lightningPrefab;
    public LightningBoltScript firePointsLightning;
    private LightningBoltScript firePointsBolt;
    private LineRenderer firePointsLine;
    private LightningBoltScript[] bolts = new LightningBoltScript[2];
    private LineRenderer[] lines = new LineRenderer[2];
    private float shootTimer;
    private Enemy currentEnemy;
    public bool isShooting = false;
    private float damageMultiplier = 1;
    public float damageIncrease;
    public bool isActiveCoroutine;
    public AudioSource pSource;

    public override void Initialize()
    {
        p = PlayerController.instance;
        pSource = p.GetComponent<AudioSource>();
        pSource.loop = true;
        pSource.clip = p.ship.primarySound;
        if (!bolts[0])
        {
            bolts[0] = Instantiate(lightningPrefab, p.firePoints[0].position, p.firePoints[0].rotation, p.transform);
            lines[0] = bolts[0].GetComponent<LineRenderer>();
            lines[0].enabled = false;
        }
        if (!firePointsBolt)
        {
            firePointsBolt = Instantiate(firePointsLightning, p.transform.position, p.transform.rotation, p.transform);

            firePointsLine = firePointsBolt.GetComponent<LineRenderer>();
            firePointsLine.useWorldSpace = true;
            firePointsBolt.StartPosition = p.firePoints[0].position - p.transform.position;
            firePointsBolt.EndPosition = p.firePoints[1].position - p.transform.position;
            firePointsLine.useWorldSpace = false;
            //firePointsLine.enabled = false;
        }
        p.shouldStopCoroutine = false;
    }

    public override IEnumerator Shoot()
    {
        if (!isActiveCoroutine)
        {
            isActiveCoroutine = true;
            yield return new WaitForSeconds(.05f);
            if (!bolts[1])
            {
                bolts[1] = Instantiate(lightningPrefab, p.firePoints[1].position, p.firePoints[1].rotation, p.transform);
                lines[1] = bolts[1].GetComponent<LineRenderer>();
                lines[1].enabled = false;
            }
            lines[0].enabled = true;
            lines[1].enabled = true;
            pSource.Play();
            while (p.fireButton.buttonPressed && p.targetEnemy)
            {
                if (!currentEnemy)
                {
                    currentEnemy = p.targetEnemy.GetComponent<Enemy>();
                    damageMultiplier = 1;
                }

                if (p.targetEnemy.transform != currentEnemy.transform)
                {
                    currentEnemy.curMoveSpeed = currentEnemy.moveSpeed;
                    currentEnemy = p.targetEnemy.GetComponent<Enemy>();
                    damageMultiplier = 1;
                }

                float distance = (currentEnemy.transform.position - p.transform.position).magnitude - .1f;
                for (int i = 0; i < 2; i++)
                {
                    bolts[i].enabled = true;
                    bolts[i].StartPosition = Vector3.zero;
                    bolts[i].EndPosition = Vector3.right * distance;
                }
                currentEnemy.curMoveSpeed = currentEnemy.moveSpeed / 2f;

                damageMultiplier += Time.deltaTime * damageIncrease;
                if (damageMultiplier > 1.5f)
                    damageMultiplier = 1.5f;
                for (int i = 0; i < 2; i++)
                {
                    lines[i].startColor = new Color(1, 1, 3f - 2 * damageMultiplier);
                    lines[i].endColor = new Color(1, 3f - 2 * damageMultiplier, 3f - 2 * damageMultiplier);
                }
                firePointsLine.startColor = lines[0].startColor;
                firePointsLine.endColor = lines[0].startColor;

                if (p.shotCounter <= 0)
                {
                    p.shotCounter = p.fireRate;
                    p.justShot = true;
                    if (currentEnemy.canTakeDamage)
                    {
                        float critChance = Random.Range(0, 100);
                        if (critChance < p.critChance)
                        {
                            currentEnemy.TakeDamage(p.firePoints.Count * p.damagePerBullet * damageMultiplier * (1 + (p.attackMultiplier + p.damageMultiplier) / 100f) * p.critMultiplier);
                            Instantiate(GameManager.instance.criticalText, currentEnemy.transform.position, Quaternion.identity);
                        }
                        else
                            currentEnemy.TakeDamage(p.firePoints.Count * p.damagePerBullet * damageMultiplier * (1 + (p.attackMultiplier + p.damageMultiplier) / 100f));
                    }
                    else
                    {
                        Instantiate(GameManager.instance.immuneText, currentEnemy.transform.position, Quaternion.identity);
                    }
                }
                yield return null;
            }
            pSource.Stop();
            firePointsLine.startColor = Color.white;
            firePointsLine.endColor = Color.white;
            damageMultiplier = 1;
            if (currentEnemy)
                currentEnemy.curMoveSpeed = currentEnemy.moveSpeed;
            currentEnemy = null;
            lines[0].enabled = false;
            lines[0].positionCount = 0;
            lines[1].enabled = false;
            lines[1].positionCount = 0;
            bolts[0].enabled = false;
            bolts[1].enabled = false;
            p.shotCounter = p.fireRate;
            isActiveCoroutine = false;
            yield return new WaitForSeconds(.06f);
            p.isShooting = false;
        }
    }
    

    public override void Reset()
    {

    }

    public override void CancelShooting()
    {
        isActiveCoroutine = false;
        firePointsLine.startColor = Color.white;
        firePointsLine.endColor = Color.white;
        damageMultiplier = 1;
        if (currentEnemy)
            currentEnemy.curMoveSpeed = currentEnemy.moveSpeed;
        currentEnemy = null;
        p.isShooting = false;
        lines[0].enabled = false;
        lines[0].positionCount = 0;
        lines[1].enabled = false;
        lines[1].positionCount = 0;
        bolts[0].enabled = false;
        bolts[1].enabled = false;
        p.shotCounter = p.fireRate;
        isActiveCoroutine = false;
    }
}
