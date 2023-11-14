using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Abilities/Dash Damage Ability"))]
public class DashAbility : Ability
{
    public float dashDistance;
    public float dashTime = .15f;
    public float damagePerDash;
    private float damage;
    public LayerMask layerMask;
    public GameObject dashTrail;
    private ParticleSystem trail;
    public GameObject colliderPrefab;
    private Collider2D activeCollider;
    public GameObject dashExplosion;

    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        cooldown = baseCooldown - .25f * level + .25f;

        damage = damagePerDash + .75f * level - .75f;
        activeCollider = Instantiate(colliderPrefab, p.transform.position, p.transform.rotation, p.transform).GetComponent<Collider2D>();
        activeCollider.enabled = false;
        activeCollider.GetComponent<DamageCollider>().damage = damage;
        activeCollider.GetComponent<DamageCollider>().impactExplosion = dashExplosion;

        trail = Instantiate(dashTrail, p.transform.position + p.transform.up * .3f, p.transform.rotation, p.transform).GetComponentInChildren<ParticleSystem>();
        trail.Stop();
    }

    public override void TriggerAbility()
    {
        trail.Play();
        activeCollider.enabled = true;
        p.moveSpeed = p.maxMoveSpeed;
        abilityCoroutineManager.StartCoroutine(Dash());
        p.canMove = false;
        p.canShoot = false;
        p.standardInput = false;
        p.canTakeDamage = false;
        //Instantiate(dashExplosion, p.transform.position ,Quaternion.Euler(p.transform.rotation.eulerAngles + Vector3.forward * 90));
    }

    public Vector3 DashPoint(float distance)
    {
        RaycastHit2D hit = Physics2D.Raycast(p.transform.position, p.transform.up, distance, layerMask);
        if (hit.collider == null)
        {
            return p.transform.position + (Vector3)p.transform.up * distance;
        }
        else
            return p.transform.position + ((Vector3)hit.point - p.transform.position) * .75f;
    }

    IEnumerator Dash()
    {
        float elapsed = 0;
        Vector3 dashPoint = DashPoint(dashDistance);
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while(elapsed < dashTime)
        {
            p.transform.position = Vector3.Lerp(p.transform.position, dashPoint, elapsed / dashTime);

            elapsed += Time.fixedDeltaTime;
            yield return wait;
        }
        activeCollider.enabled = false;
        p.canMove = true;
        p.canShoot = true;
        p.standardInput = true;
        p.canTakeDamage = true;
        trail.Stop();
    }

    public override void UpdateAbility()
    {
        throw new System.NotImplementedException();
    }

    public override void ToActivateUpdate()
    {
        throw new System.NotImplementedException();
    }

    public override void CancelUpdateAbility()
    {

    }
}
