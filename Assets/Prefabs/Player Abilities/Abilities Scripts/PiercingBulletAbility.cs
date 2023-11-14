using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Piercing Bullet")]
public class PiercingBulletAbility : Ability
{
    public Bullet bulletPrefab;
    private bool startedAbility;
    [HideInInspector]
    public Vector2 pMoveDirection;
    float pTurningSpeed;
    float angle;
    public float damagePerBullet;
    [HideInInspector]
    public float damage;
    public LineRenderer linePrefab;
    [HideInInspector]
    public LineRenderer line;
    public AudioClip sound;

    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        cooldown = baseCooldown - .25f * level + .25f;
        damage = damagePerBullet + 2 * level - 2;
        line = Instantiate(linePrefab, p.transform.position, p.transform.rotation, p.transform);
        line.enabled = false;
        line.useWorldSpace = true;

    }
    public override void UpdateAbility()
    {
        //Vector2 tempDirection = Vector2.up * p.movementJoystick.Vertical + Vector2.right * p.movementJoystick.Horizontal;
        if (p.moveDirection != Vector2.zero)
            pMoveDirection = p.moveDirection;
        angle = Mathf.Atan2(pMoveDirection.y, pMoveDirection.x) * Mathf.Rad2Deg - 90;
        p.transform.rotation = Quaternion.Lerp(p.transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), pTurningSpeed * Time.fixedDeltaTime);

        line.SetPosition(0, p.transform.position);
        RaycastHit2D hit = Physics2D.Raycast(p.transform.position, p.transform.up, Mathf.Infinity, LayerMask.GetMask("Default") | LayerMask.GetMask("Obstacles"));
        line.SetPosition(1, hit.point);
    }
    public override void ToActivateUpdate()
    {
        canBeUsedWhileMoving = true;
        canBeUsedWhileShooting = true;
        p.abilityUpdateBool = true;
        p.standardInput = false;
        p.canMove = false;
        p.canShoot = false;
        p.cameraAheadOfPlayer = true;
        pMoveDirection = p.lastMoveDirection;
        line.enabled = true;
        startedAbility = true;
        pTurningSpeed = p.turningSpeed;
        p.turningSpeed = 0;
    }
    public override void CancelUpdateAbility()
    {
        p.shouldTurn = true;
        p.abilityUpdateBool = false;
        p.cameraAheadOfPlayer = false;
        abilityManager.GoOnCooldown();
        abilityManager.abilityUpdateOn = false;
        if (startedAbility)
        {
            ShootBullet();
            abilityCoroutineManager.StartCoroutine(PushBack());
        }
        startedAbility = false;
        canBeUsedWhileMoving = false;
        canBeUsedWhileShooting = false;
        line.enabled = false;
    }

    void ShootBullet()
    {
        SoundManager.instance.soundSource.PlayOneShot(sound);
        Instantiate(bulletPrefab.muzzleFlash, p.transform.position + p.transform.up * .15f, Quaternion.Euler(p.transform.rotation.eulerAngles + Vector3.forward * 90));
        Bullet bullet = Instantiate(bulletPrefab, p.transform.position + p.transform.up * .15f, Quaternion.Euler(p.transform.rotation.eulerAngles + Vector3.forward * 90));
        BulletSetup(bullet);
    }

    public void BulletSetup(Bullet bullet)
    {
        bullet.canPierce = true;
        bullet.limitedPiercings = false;
        bullet.damage = damage;
        bullet.pushBack = 6;

        bullet.whoShotIt = p.transform;
        bullet.isCrit = false;

        bullet.GetComponent<TrailBullet>().smallBulletDamage = bullet.damage / 3f;

    }

    IEnumerator PushBack()
    {
        p.rb.AddForce(-p.transform.up * 6f, ForceMode2D.Impulse);
        yield return new WaitForSeconds(0.3f);
        if (p != null)
        {
            p.rb.velocity = Vector2.zero;
            p.standardInput = true;
            p.canMove = true;
            p.canShoot = true;
            p.turningSpeed = pTurningSpeed;
        }
    }
    public override void TriggerAbility()
    {
        throw new System.NotImplementedException();
    }
}
