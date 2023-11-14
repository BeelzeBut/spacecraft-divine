using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = ("Abilities/Dash Frenzy Ability"))]
public class DashFrenzyAbility : Ability
{
    int numberOfDashes;
    public float damagePerDash;
    float damage;
    public float chargeTime;
    Enemy[] closestEnemies;
    public Transform trailTransform;
    TrailRenderer trail;
    public float dashRange;
    public Transform dashShieldPrefab;
    GameObject dashShield;

    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        damage = damagePerDash + .5f * level - .5f;
        numberOfDashes = 2 + level;
        closestEnemies = new Enemy[numberOfDashes];
        cooldown = baseCooldown + .5f * level - .5f;
        trail = Instantiate(trailTransform, p.transform.position, p.transform.rotation, p.transform).GetComponent<TrailRenderer>();
        trail.enabled = false;
        trail.material.SetTexture("_MainTex", p.ship.shipSprite.texture);
        p.spaceshipObjects.Add(trail.transform);
        p.transform.rotation = Quaternion.identity;
        dashShield = Instantiate(dashShieldPrefab, p.transform.position + p.transform.up * dashShieldPrefab.transform.position.y, p.transform.rotation).gameObject;
        dashShield.transform.SetParent(p.transform);
        p.spaceshipObjects.Add(dashShield.transform);
        dashShield.SetActive(false);
    }

    public override void TriggerAbility()
    {
        FindClosestEnemies();
        abilityCoroutineManager.DashFrenzy(closestEnemies, chargeTime, numberOfDashes, trail, damage, dashShield, dashRange);
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
        throw new System.NotImplementedException();
    }

    public void FindClosestEnemies()
    {
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        allEnemies = allEnemies.OrderBy(x => (x.transform.position - p.transform.position).sqrMagnitude).ToArray();

        for (int i = 0; i < Mathf.Min(numberOfDashes, allEnemies.Length); i++)
        {

            if ((allEnemies[i].transform.position - p.transform.position).sqrMagnitude <= dashRange * dashRange)
            {
                closestEnemies[i] = allEnemies[i];
            }
        }
    }
}