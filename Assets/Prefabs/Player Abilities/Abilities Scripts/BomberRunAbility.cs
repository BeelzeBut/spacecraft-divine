using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Bomber Run")]
public class BomberRunAbility : Ability
{
    public Transform rangeAreaPrefab;
    private Transform rangeIndicator;
    private bool startedAbility;
    public float rangeIncrease;
    public float distance = 1f;
    float pMaxMoveSpeed;
    [HideInInspector]
    public Vector2 pMoveDirection;
    float pTurningSpeed;
    float angle;
    public float playerSpeed = 20;
    public Explosion explosionPrefab;
    public float damagePerExplosion;
    [HideInInspector]
    public float damage;
    public TrailRenderer trailPrefab;
    [HideInInspector]
    public TrailRenderer trail;

    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        cooldown = baseCooldown - .5f * level + .5f;
        damage = damagePerExplosion + 1.75f * level - 1.75f;
        trail = Instantiate(trailPrefab, p.transform.position, p.transform.rotation, p.transform);
        trail.enabled = false;
        trail.material.SetTexture("_MainTex", p.ship.shipSprite.texture);

    }
    public override void UpdateAbility()
    {
        if (rangeIndicator.localScale.y < 6)
        {
            rangeIndicator.localScale += Vector3.up * rangeIncrease * Time.deltaTime * 3;
            distance = rangeIndicator.localScale.y * .7f;
        }
        
        if (p.moveDirection != Vector2.zero)
            pMoveDirection = p.moveDirection;
        angle = Mathf.Atan2(pMoveDirection.y, pMoveDirection.x) * Mathf.Rad2Deg - 90;
        p.transform.rotation = Quaternion.Lerp(p.transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), pTurningSpeed * Time.fixedDeltaTime);
    }
    public override void ToActivateUpdate()
    {
        canBeUsedWhileMoving = true;
        canBeUsedWhileShooting = true;
        p.abilityUpdateBool = true;
        p.standardInput = false;
        pMaxMoveSpeed = p.maxMoveSpeed;
        p.canMove = false;
        p.canShoot = false;
        p.cameraAheadOfPlayer = true;
        pMoveDirection = p.lastMoveDirection;
        rangeIndicator = Instantiate(rangeAreaPrefab, p.transform.position, p.transform.rotation, p.transform);
        startedAbility = true;
        pTurningSpeed = p.turningSpeed;
        p.turningSpeed = 0;
    }
    public override void CancelUpdateAbility()
    {
        p.shouldTurn = true;
        p.abilityUpdateBool = false;
        abilityManager.GoOnCooldown();
        abilityManager.abilityUpdateOn = false;
        p.canMove = true;
        p.cameraAheadOfPlayer = false;
        p.turningSpeed = pTurningSpeed;
        if (startedAbility)
        {
            trail.enabled = true;
            abilityCoroutineManager.BomberRun(distance, this, rangeIndicator);
            p.angle = angle;
        }
        startedAbility = false;
        canBeUsedWhileMoving = false;
        canBeUsedWhileShooting = false;
    }
    public override void TriggerAbility()
    {
        throw new System.NotImplementedException();
    }

    public bool CanDash(float distance)
    {
        RaycastHit2D hit = Physics2D.Raycast(p.transform.position, pMoveDirection, distance, LayerMask.GetMask("Default"));
        if (hit.collider == null)
        {
            if (!Physics2D.OverlapCircle((Vector2)p.transform.position + pMoveDirection * distance, .05f, LayerMask.GetMask("Obstacles")))
                return true;
            else return false;
        }
        else
            return false;
    }
}
