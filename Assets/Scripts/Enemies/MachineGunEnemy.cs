using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineGunEnemy : Enemy
{
    public Transform[] firePoints;
    public LineRenderer linePrefab;
    private LineRenderer[] lines = new LineRenderer[2];
    public Transform linesFirePoint;
    public float chargeTime = .8f;
    public float loseFocusAfterCooldown = 1.25f;
    public Bullet bulletPrefab;
    public float fireRate;
    public float spread;
    private float initialTurningSpeed;
    private int lastFirePoint = -1, secondLastFirePoint = -1;
    public bool isEnhanced = false;
    public float stopShootingAfter = 1.5f;

    void Start()
    {
        MaterialSetup();
        shootTimer = Random.Range(1, shootCooldown);
        player = PlayerController.instance;
        curMoveSpeed = moveSpeed;

        lines[0] = Instantiate(linePrefab, transform.position, transform.rotation, transform);
        lines[0].enabled = false;
        lines[1] = Instantiate(linePrefab, transform.position, transform.rotation, transform);
        lines[1].enabled = false;
        initialTurningSpeed = turningspeed;
        initialScale = transform.localScale;
    }

    void Update()
    {
        if (player.isAlive)
        {
            EnemyAI();

            if (shouldShoot)
            {
                StartCoroutine(StartShooting());
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
    public override IEnumerator StopAttack()
    {
        lines[0].enabled = false;
        lines[1].enabled = false;
        linesFirePoint.localRotation = Quaternion.Euler(0, 0, -90);
        turningspeed = initialTurningSpeed;

        return base.StopAttack();
    }

    IEnumerator StartShooting()
    {
        shouldShoot = false;
        shootTimer = 1000;
        canMove = false;
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
        turningspeed *= 10f;
        while (elapsed < chargeTime)
        {
            lines[0].SetPosition(0, linesFirePoint.position);
            lines[1].SetPosition(0, linesFirePoint.position);

            linesFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90 + deviation);
            hit = Physics2D.Raycast(linesFirePoint.position, linesFirePoint.right, 1000f, roomLayermask | obstacleLayermask);
            lines[0].SetPosition(1, hit.point);
            linesFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90 - deviation);
            hit = Physics2D.Raycast(linesFirePoint.position, linesFirePoint.right, 1000f, roomLayermask | obstacleLayermask);
            lines[1].SetPosition(1, hit.point);

            deviation -= 30 * 1 / chargeTime * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        lines[1].enabled = false;
        deviation = 0;
        linesFirePoint.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - 90 + deviation);
        hit = Physics2D.Raycast(linesFirePoint.position, linesFirePoint.right, 1000f, roomLayermask | obstacleLayermask);
        lines[0].SetPosition(1, hit.point);
        hit = Physics2D.Raycast(linesFirePoint.position, linesFirePoint.right, 1000f, roomLayermask | obstacleLayermask);
        lines[0].startColor = Color.white;
        lines[0].endColor = Color.white;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        float shootCounter = 0;
        float lookCooldown = loseFocusAfterCooldown;
        float stopShooting = stopShootingAfter;
        while (lookCooldown > 0 && stopShooting > 0)
        {
            hit = Physics2D.CircleCast(linesFirePoint.position, .05f, linesFirePoint.right, 100f, roomLayermask | obstacleLayermask | LayerMask.GetMask("Player"));
            if (hit.collider.CompareTag("Player"))
            {
                lookCooldown = loseFocusAfterCooldown;
            }
            else
            {
                lookCooldown -= Time.fixedDeltaTime;
            }
            lines[0].SetPosition(0, linesFirePoint.position);
            lines[0].SetPosition(1, hit.collider ? hit.point : (Vector2)(transform.position + linesFirePoint.right * 30f));
            lines[0].startColor = new Color(1, 1, 1, lookCooldown);
            lines[0].endColor = lines[0].startColor;
            shootCounter -= Time.fixedDeltaTime;
            if (shootCounter <= 0)
            {
                shootCounter = fireRate;
                float spread = this.spread * Random.Range(-1f, 1f) * 7.5f;
                int firePointNumber = Random.Range(0, firePoints.Length);
                while (firePointNumber == lastFirePoint || firePointNumber == secondLastFirePoint)
                {
                    firePointNumber = Random.Range(0, firePoints.Length);
                }
                secondLastFirePoint = lastFirePoint;
                lastFirePoint = firePointNumber;
                PlaySound(attackSound);
                Instantiate(muzzleFlash, firePoints[firePointNumber].position, Quaternion.Euler(firePoints[firePointNumber].rotation.eulerAngles + Vector3.forward * spread));
                Bullet bullet = Instantiate(bulletPrefab, firePoints[firePointNumber].position, Quaternion.Euler(firePoints[firePointNumber].rotation.eulerAngles + Vector3.forward * spread));
                bullet.whoShotIt = transform;
            }
            stopShooting -= Time.fixedDeltaTime;
            yield return wait;
        }
        lines[0].enabled = false;

        yield return new WaitForSeconds(.25f);

        turningspeed = initialTurningSpeed;
        canMove = true;
        canBePushedBack = true;
        shootTimer = Random.Range(3 / 4f * shootCooldown, 5 / 4f * shootCooldown);
        ChooseBehaviour(1);
    }
}
