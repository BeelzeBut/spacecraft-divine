using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName = ("Abilities/Dash Ability"))]
public class TeleportAbility : Ability
{
    public float dashDistance;
    public float moveSpeedIncrease = 30f;
    public LayerMask layerMask;
    [SerializeField]
    public GameObject teleportStart, teleportEnd;

    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        cooldown = baseCooldown - .5f * level + .5f;
        dashDistance = 2 + .2f * level - .2f;
    }

    public override void TriggerAbility()
    {
        Dash(dashDistance);
    }

    public bool CanDash(float distance)
    { 
        RaycastHit2D hit = Physics2D.Raycast(p.transform.position, p.lastMoveDirection, distance, layerMask);
        if (hit.collider == null)
        {
            if (!Physics2D.OverlapCircle((Vector2)p.transform.position + p.lastMoveDirection * distance, .05f, LayerMask.GetMask("Obstacles")))
                return true;
            else return false;
        }
        else
            return false;
    }

    void Dash(float distance)
    {
        p.moveSpeed = p.maxMoveSpeed;
        abilityCoroutineManager.Dash(distance, this);
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
