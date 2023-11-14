using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeployerEnemy : Enemy
{
    [Header("Deploy")]
    public Transform bulletStartExplosion;
    public Transform[] deployFirePoints;
    public Bullet targetedBulletPrefab;
    public int numberOfDeployedBullets;
    public float deployFireRate;

    [Header("Triple Bombs")]
    public float bombsFireRate;
    public ArchedBullet bombPrefab;
    public Transform bombFirePoint;

    [Header("Charged Beam")]
    public Transform beamMuzzleFlash;
    public BeamBullet beamPrefab;
    public float chargeTime;
    float initialTurningSpeed;
    public LineRenderer[] lines;
    public Transform beamFirePoint;
    public ParticleSystem chargeUp;
    public float beamDamage;

    public AudioClip deploySound, deploySoundSetup, bombsSound, beamSound, chargeSound;

    void Start()
    {
        MaterialSetup();

        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;

        initialScale = transform.localScale;
        transform.localScale = Vector3.zero;
        initialTurningSpeed = turningspeed;
        source.clip = chargeSound;
    }

    // Update is called once per frame
    void Update()
    {
        if (player.isAlive)
        {
            EnemyAI();

            if (shouldShoot)
            {
                ChooseAttack();
            }
        }
    }

    private void FixedUpdate()
    {
        if (player.isAlive)
        {
            EnemyAIFixedUpdate();
        }
    }

    void ChooseAttack()
    {
        shouldShoot = false;
        shootTimer = 10f;
        int k = Random.Range(0, 3);

        switch (k)
        {
            case 0:
                StartCoroutine(Deploy());
                break;
            case 1:
                StartCoroutine(TripleBombs());
                break;
            case 2:
                StartCoroutine(ChargedBeam());
                break;
        }
    }

    public override IEnumerator StopAttack()
    {
        chargeUp.Stop();
        lines[0].enabled = false;
        lines[1].enabled = false;
        return base.StopAttack();
    }
    IEnumerator ChargedBeam()
    {
        shouldShoot = false;
        shootTimer = 10;
        canMove = false;
        chargeUp.Play();
        canBePushedBack = false;
        float elapsed = 0;

        lines[0].enabled = true;
        lines[1].enabled = true;
        lines[0].startColor = Color.red;
        lines[1].startColor = Color.red;
        lines[0].endColor = Color.red;
        lines[1].endColor = Color.red;
        RaycastHit2D hit;
        float deviation = 30;
        turningspeed *= 2f;
        source.Play();
        while (elapsed < chargeTime)
        {

            lines[0].SetPosition(0, beamFirePoint.position);
            lines[1].SetPosition(0, beamFirePoint.position);

            beamFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90 + deviation);
            hit = Physics2D.Raycast(beamFirePoint.position, beamFirePoint.right, 1000f, roomLayermask | obstacleLayermask);
            lines[0].SetPosition(1, hit.point);
            beamFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90 - deviation);
            hit = Physics2D.Raycast(beamFirePoint.position, beamFirePoint.right, 1000f, roomLayermask | obstacleLayermask);
            lines[1].SetPosition(1, hit.point);

            deviation -= 30 * 1 / chargeTime * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        deviation = 0;
        beamFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90 + deviation);
        hit = Physics2D.Raycast(beamFirePoint.position, beamFirePoint.right, 1000f, roomLayermask | obstacleLayermask);
        lines[0].SetPosition(1, hit.point);
        beamFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90 - deviation);
        hit = Physics2D.Raycast(beamFirePoint.position, beamFirePoint.right, 1000f, roomLayermask | obstacleLayermask);
        lines[1].SetPosition(1, hit.point);
        lines[0].startColor = Color.white;
        lines[1].startColor = Color.white;
        lines[0].endColor = Color.white;
        lines[1].endColor = Color.white;
        canLook = false;
        yield return new WaitForSeconds(.2f);

        chargeUp.Stop();
        source.Stop();
        lines[0].enabled = false;
        lines[1].enabled = false;

        PlaySound(beamSound);
        Instantiate(beamMuzzleFlash, beamFirePoint.position, beamFirePoint.rotation);
        BeamBullet beam = Instantiate(beamPrefab, beamFirePoint.position, beamFirePoint.rotation);
        beam.charged = 1;
        beam.damage = beamDamage;
        beam.whoShotIt = transform;

        rb.AddForce(5f * transform.up, ForceMode2D.Impulse);

        yield return new WaitForSeconds(.5f);
        turningspeed = initialTurningSpeed;
        canLook = true;
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        canMove = true;
        canBePushedBack = true;
        ChooseBehaviour(1);
    }
    IEnumerator TripleBombs()
    {
        shouldShoot = false;
        shootTimer = 10f;
        canMove = false;
        canBePushedBack = false;
        canLook = false;

        for(int i = 0; i < 3; i++)
        {
            float deviation = -0.6f * i;
            PlaySound(bombsSound);
            for (int j = 0; j < i + 1; j++)
            {
                ArchedBullet bullet = Instantiate(bombPrefab, bombFirePoint.position, Quaternion.Euler(0, 0, 90));
                bullet.whoShotIt = transform;
                bullet.targetPosition = transform.position - transform.up * (i + 1) * 1.375f + transform.right * deviation;
                deviation += 1.2f;
            }
            if(i < 2)
                yield return new WaitForSeconds(bombsFireRate);
        }

        yield return new WaitForSeconds(.5f);
        canMove = true;
        canLook = true;
        canBePushedBack = true;
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        ChooseBehaviour(1);
    }
    IEnumerator Deploy()
    {
        shouldShoot = false;
        shootTimer = 10f;
        canMove = false;
        canBePushedBack = false;

        int nr = Random.Range(numberOfDeployedBullets - 1, numberOfDeployedBullets + 2);
        WaitForSeconds wait = new WaitForSeconds(deployFireRate);
        for(int i = 0; i < nr; i++)
        {
            PlaySound(deploySound);
            Bullet bullet = Instantiate(targetedBulletPrefab, deployFirePoints[i % 2].position, Quaternion.Euler(deployFirePoints[i % 2].rotation.eulerAngles + Vector3.forward * Random.Range(-90f, 90f)));
            bullet.whoShotIt = transform;
            bullet.StartCoroutine(TargetedBulletSetup(bullet, deploySoundSetup));
            yield return wait; 
        }

        yield return new WaitForSeconds(.2f);
        canMove = true;
        canBePushedBack = true;
        shootTimer = Random.Range(0, shootCooldown * 1.25f);
        ChooseBehaviour(1);
    }

    IEnumerator TargetedBulletSetup(Bullet bullet, AudioClip sound)
    {
        bullet.trail.enabled = false;
        Vector3 initialScale = bullet.transform.localScale;
        bullet.transform.localScale = Vector3.zero;
        float elapsed = 0;
        while(elapsed < .3f)
        {
            bullet.transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, elapsed / .3f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        bullet.transform.localScale = initialScale;
        yield return new WaitForSeconds(.45f);
        if (bullet.gameObject.layer == 11)
        {
            PlaySound(sound);
            Instantiate(bulletStartExplosion, bullet.transform.position, bullet.transform.rotation);
            bullet.trail.enabled = true;
            Vector3 lookDir = player.transform.position - bullet.transform.position;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
            bullet.speed *= 4;
        }
    }
}
