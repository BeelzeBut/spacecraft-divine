using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityCoroutineManager : MonoBehaviour
{
    public static AbilityCoroutineManager instance;
    PlayerController p;
    public LayerMask layermask;
    Material simpleAlphaBlend;

    private Vector3 dashVel;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        p = PlayerController.instance;
    }

    //Dash
    public void Dash(float distance, TeleportAbility dashAbility)
    {
        StartCoroutine(DashC(distance, dashAbility));
    }
    IEnumerator DashC(float distance, TeleportAbility dashAbility)
    {
        p.canShoot = false;
        CameraShake.instance.StartShake(.15f, .1f);
        Instantiate(dashAbility.teleportStart, p.transform.position, p.transform.rotation);
        float elapsed = 0;
        Vector3 pScale = p.transform.localScale;
        float pMaxSpeed = p.maxMoveSpeed;
        p.maxMoveSpeed = 0;

        while (elapsed < .25f)
        {
            p.transform.localScale = Vector3.Lerp(p.transform.localScale, Vector3.zero, elapsed / .25f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (dashAbility.CanDash(distance))
        {
            p.transform.position += (Vector3)p.lastMoveDirection * distance;
        }
        else
        {
            RaycastHit2D hit;
            Vector3 direction;
            if (p.moveDirection != Vector2.zero)
            {
                direction = p.moveDirection;
            }
            else
            {
                direction = p.lastMoveDirection;
            }
            hit = Physics2D.Raycast(p.transform.position, direction, distance, dashAbility.layerMask);
            if (hit.collider != null)
                p.transform.position = hit.point;
            else
            {
                hit = Physics2D.Raycast(p.transform.position, direction, distance, LayerMask.GetMask("Obstacles"));
                p.transform.position = hit.point;
            }

        }
        Instantiate(dashAbility.teleportEnd, p.transform.position, Quaternion.Euler(p.transform.rotation.eulerAngles * -1));

        elapsed = 0;
        p.maxMoveSpeed = pMaxSpeed;
        while (elapsed < .1f)
        {
            p.transform.localScale = Vector3.Lerp(Vector3.zero, pScale, elapsed / .1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        p.transform.localScale = pScale;
        p.canShoot = true;
    }

    //Shield
    public void SpawnShield(GameObject activeShield, float duration)
    {
        StartCoroutine(SpawnShieldC(activeShield, duration));
    }

    IEnumerator SpawnShieldC(GameObject activeShield, float duration)
    {
        Vector3 shieldScale = activeShield.transform.localScale;
        activeShield.transform.localScale = Vector3.zero;
        float elapsed = 0;
        while (elapsed < duration)
        {
            activeShield.transform.localScale = Vector3.Lerp(Vector3.zero, shieldScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        activeShield.transform.localScale = shieldScale;
    }

    public void DespawnShield(GameObject activeShield, float duration)
    {
        StartCoroutine(DespawnShieldC(activeShield, duration));
    }

    IEnumerator DespawnShieldC(GameObject activeShield, float duration)
    {
        float elapsed = 0;

        while (elapsed < duration)
        {
            activeShield.transform.localScale = Vector3.Lerp(activeShield.transform.localScale, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(activeShield);
    }

    //Dash Frenzy
    public void DashFrenzy(Enemy[] closestEnemies, float chargeTime, int numberOfDashes, TrailRenderer trail, float damage, GameObject dashShield, float range)
    {
        StartCoroutine(DashFrenzyC(closestEnemies, chargeTime, numberOfDashes, trail, damage, dashShield, range));
    }
    public IEnumerator DashFrenzyC(Enemy[] closestEnemies, float chargeTime, int numberOfDashes, TrailRenderer trail, float damage, GameObject dashShield, float range)
    {
        p.moveDirection = Vector2.zero;
        CameraMovement.instance.delay *= .5f;
        SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.abilitySounds[1]);
        p.solidCollider.enabled = false;
        trail.enabled = true;
        p.invincibility = 5f;
        p.standardInput = false;
        p.canMove = false;
        foreach(Transform thruster in p.thrusters)
        {
            thruster.gameObject.SetActive(true);
        }
        CameraMovement.instance.targetEnemy = null;
        if (closestEnemies[0])
            PlayerLookAt(closestEnemies[0].transform);
        float elapsed = 0;
        dashShield.SetActive(true);
        dashShield.transform.localScale = Vector3.zero;
        while (dashShield.transform.localScale != new Vector3(.8f, 2, 1))
        {
            //dashShield.transform.localScale = Vector3.Lerp(new Vector3(0f, 2, 0), new Vector3(.8f, 2, 1), elapsed / chargeTime);
            dashShield.transform.localScale = Vector3.MoveTowards(dashShield.transform.localScale, new Vector3(.8f, 2, 1), Time.deltaTime * 8);
            elapsed += Time.deltaTime;
            yield return null;
        }
        //dashShield.transform.localScale = new Vector3(.8f, 2, 1);
        dashShield.GetComponent<Animator>().SetTrigger("rotate");
        yield return new WaitForSeconds(chargeTime / 2f);
        //int stockedUp = 0;
                
        if (closestEnemies[0])
        {
            PlayerLookAt(closestEnemies[0].transform);
        }
       
        int maxNumberOfDashes = numberOfDashes;
        while(numberOfDashes > 0)
        {
            bool hasFoundEnemies = false;
            for (int i = 0; i < maxNumberOfDashes && numberOfDashes > 0; i++)
            {          
                if (closestEnemies[i])// && (closestEnemies[i].transform.position - p.transform.position).magnitude <= range)
                {
                    p.canMove = false;
                    p.canShoot = false;
                    p.standardInput = false;
                    SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.abilitySounds[0]);
                    hasFoundEnemies = true;
                    PlayerLookAt(closestEnemies[i].transform);
                    Vector3 moveDir = (closestEnemies[i].transform.position - p.transform.position).normalized;
                    Vector3 enemyBackPos = closestEnemies[i].transform.position + moveDir;
                    RaycastHit2D[] hit = new RaycastHit2D[1];
                    Physics2D.RaycastNonAlloc(p.transform.position, (enemyBackPos - p.transform.position), hit, (enemyBackPos - p.transform.position).magnitude, layermask);
                    if (hit[0].collider)
                    {
                        enemyBackPos = (Vector2)p.transform.position + new Vector2((hit[0].point.x - p.transform.position.x) * .9f, (hit[0].point.y - p.transform.position.y) * .9f);
                    }
                    //p.transform.position = enemyBackPos;
                    while (p.transform.position != enemyBackPos)
                    {
                        p.transform.position = Vector3.MoveTowards(p.transform.position, enemyBackPos, 30f * Time.deltaTime);
                        yield return null;
                    }
                    if(closestEnemies[i].canTakeDamage)
                        closestEnemies[i].TakeDamage(damage * (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f));
                    else
                    {
                        Instantiate(GameManager.instance.immuneText, closestEnemies[i].transform.position, Quaternion.identity);
                    }
                    CameraShake.instance.StartShake(.15f, .1f);
                    numberOfDashes--;
                    yield return new WaitForSeconds(chargeTime / 3f);
                }
            }
            if(!hasFoundEnemies)
            {
                break;
            }
        }
        p.standardInput = true;
        p.canMove = true;
        p.canShoot = true;
        trail.enabled = false;
        foreach (Transform thruster in p.thrusters)
        {
            thruster.gameObject.SetActive(false);
        }
        dashShield.SetActive(false);
        p.solidCollider.enabled = true;
        CameraMovement.instance.delay *= 2f;

        yield return new WaitForSeconds(.3f);
        p.invincibility = 0;

    }

    //Plasma Bullet
    public void SmallBulletsPulse(FragBullet plasmaBullet, int numberOfSmallBullets)
    {
        StartCoroutine(SmallBulletsPulseC(plasmaBullet, numberOfSmallBullets));
    }

    IEnumerator SmallBulletsPulseC(FragBullet plasmaBullet, int numberOfSmallBullets)
    {
        FragBullet plasmaBulletInstance = plasmaBullet;
        float elapsed = 0;
        yield return new WaitForSeconds(.2f);
        while (plasmaBulletInstance != null && elapsed <= 2f)
        {
            plasmaBullet.Detonate(numberOfSmallBullets);
            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(.3f);
        }
    }
    private Coroutine cannonOnPlayerCoroutine;
    public void CannonOnPlayer(Transform cannon, Vector3 cannonInitialPos)
    {
        StopCannonOnPlayer();
        cannonOnPlayerCoroutine = StartCoroutine(CannonOnPlayerC(cannon, cannonInitialPos));
    }
    public void StopCannonOnPlayer()
    {
        if (cannonOnPlayerCoroutine != null)
        {
            StopCoroutine(cannonOnPlayerCoroutine);
            cannonOnPlayerCoroutine = null;
        }
    }
    public IEnumerator CannonOnPlayerC(Transform cannon, Vector3 cannonInitialPos)
    {
        while (cannon)
        {
            cannon.position = p.transform.position + p.transform.up * cannonInitialPos.y;
            yield return null;
        }
    }
    public void GoInvis(Material mat)
    {
        StartCoroutine(GoInvisC(mat));
    }
    
    public void LeaveInvis(Material mat)
    {
        StartCoroutine(LeaveInvisC(mat));
    }
    public IEnumerator GoInvisC(Material mat)
    {
        p.spriteRenderer.material = mat;
        mat = p.spriteRenderer.material;
        mat.SetFloat("_DissolveAmount", 0);
        float elapsed = 0;
        Shadow shadow = p.GetComponentInChildren<Shadow>();
        while(mat.GetFloat("_DissolveAmount") < 1)
        {
            mat.SetFloat("_DissolveAmount", elapsed);
            Color blackness = new Color(0, 0, 0, 0.6f - elapsed * .5f);
            shadow.sprite.color = blackness;
            elapsed += 1.375f * Time.deltaTime;
            yield return null;
        }
        shadow.sprite.color = new Color(0, 0, 0, 0.1f);
        mat.SetFloat("_DissolveAmount", 1);
        p.thrusters[0].GetComponent<SpriteRenderer>().enabled = false;

        p.shouldBeAttacked = false;
        foreach(TargetedBullet bullet in FindObjectsOfType<TargetedBullet>())
        {
            if (bullet.target == p.transform)
                bullet.target = null;
        }
    }
    public IEnumerator LeaveInvisC(Material mat)
    {
        StopCoroutine("GoInvisC");
        p.shouldBeAttacked = true;
        mat = p.spriteRenderer.material;
        p.thrusters[0].GetComponent<SpriteRenderer>().enabled = true;
        mat.SetFloat("_DissolveAmount", 1);
        Shadow shadow = p.GetComponentInChildren<Shadow>();
        float elapsed = 1;
        while (mat.GetFloat("_DissolveAmount") > 0)
        {
            mat.SetFloat("_DissolveAmount", elapsed);
            elapsed -= 1.375f * Time.deltaTime;
            Color blackness = new Color(0, 0, 0, 0.6f - elapsed * .5f);
            shadow.sprite.color = blackness;
            yield return null;
        }
        shadow.sprite.color = new Color(0, 0, 0, 0.6f);
        mat.SetFloat("_DissolveAmount", 0);
        p.spriteRenderer.material = p.material;
        p.shouldBeAttacked = true;
    }

    public void BomberRun(float distance, BomberRunAbility ability, Transform indicator)
    {
        StartCoroutine(BomberRunC(distance, ability, indicator));
    }

    IEnumerator BomberRunC(float distance, BomberRunAbility ability, Transform indicator)
    {
        p.canTakeDamage = false;

        if (ability.CanDash(distance))
        {
            StartCoroutine(MovePlayer(p.transform.position + (Vector3)ability.pMoveDirection * distance, ability, distance));
        }
        else
        {
            RaycastHit2D hit;
            Vector3 direction = (Vector3)ability.pMoveDirection;

            hit = Physics2D.Raycast(p.transform.position, direction, distance, LayerMask.GetMask("Default"));
            if (hit.collider == null)
                hit = Physics2D.Raycast(p.transform.position, direction, distance, LayerMask.GetMask("Obstacles"));
            StartCoroutine(MovePlayer(hit.point, ability, ((Vector3)hit.point - p.transform.position).magnitude));
        }
        Destroy(indicator.gameObject);
        yield return null;
    }
    IEnumerator MovePlayer(Vector3 targetPos, BomberRunAbility ability, float distance)
    {
        p.GetComponentsInChildren<Collider2D>()[1].enabled = false;
        Vector3 initialPos = p.transform.position;
        Vector3 direction = (targetPos - initialPos).normalized;
        p.canMove = false;
        p.standardInput = false;
        CameraMovement.instance.delay = .025f;
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while((p.transform.position - targetPos).sqrMagnitude > .125f)
        {
            p.rb.MovePosition(p.rb.position + (Vector2)direction * ability.playerSpeed * Time.deltaTime);
            yield return wait;
        }
        p.GetComponentsInChildren<Collider2D>()[1].enabled = true;

        yield return new WaitForSeconds(.15f);
        CameraMovement.instance.delay = .0875f;
        p.canTakeDamage = true;
        p.canShoot = true;
        p.canMove = true;
        p.standardInput = true;
        ability.trail.enabled = false;

        yield return new WaitForSeconds(.25f);

        int numberOfExplosions = 1;
        if (distance < 3.6f && distance > 2.2f)
            numberOfExplosions = 2;
        else if (distance >= 3.4f)
            numberOfExplosions = 3;

        Explosion explosion = Instantiate(ability.explosionPrefab, initialPos + direction * .64f, Quaternion.identity);
        explosion.damage = ability.damage;
        for(int i = 1; i < numberOfExplosions; i++)
        {
            yield return new WaitForSeconds(.06f);
            explosion = Instantiate(ability.explosionPrefab, initialPos + direction * .64f * (3 * i - (i == 1 ? 0 : 1)), Quaternion.identity);
            explosion.damage = ability.damage;
        }

    }
    void PlayerLookAt(Transform target)
    {
        Vector3 lookDir = target.position - p.transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90;
        p.angle = angle;
    }

}
