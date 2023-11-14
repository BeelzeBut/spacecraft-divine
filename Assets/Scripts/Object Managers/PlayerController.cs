using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public PlayerControls controls;
    public bool controllerOn = false;

    public static PlayerController instance;
    public Rigidbody2D rb;
    public CircleCollider2D solidCollider;
    public LayerMask layermask;
    public GameManager gm;
    public bool testBuild = true;
    public bool testWindow = true;
    public bool isAlive = true;
    public bool isShooting = false;
    public bool justShot = false;
    public bool shouldBeAttacked = true;
    public int firePointNumber;
    private bool loadingNextLevel = false;
    public bool cameraAheadOfPlayer = false;

    public ButtonScript fireButton;
    public Button secondaryFireButton; 
    public Button interactButton;
    public FloatingJoystick movementJoystick;
    public Transform target;
    public Collectible interactableObject;

    [Header ("Spaceship")]
    public GameObject manager;
    public Ability ability;
    public AbilityCooldown abilityManager;
    public Spaceship ship;
    public ShipSelect shipSelect;
    public SpriteRenderer spriteRenderer;
    public Material material;
    public float health;
    public float maxHealth;
    public float fireRate;
    public List<Transform> firePoints;
    public float attackRange;
    public float bulletsShot;
    public float spread;
    public float maxMoveSpeed;
    public float damagePerBullet;
    public Bullet bulletPrefab;
    public List<Transform> thrusters;
    public string shipName;
    public int numberOfBursts;
    public float waitTimeBetweenBursts;
    public float critChance;
    public float critMultiplier = 2f;
    public List<Transform> spaceshipObjects;
    public bool abilityUpdateBool = false;
    public bool canTakeDamage = true;
    public float damageTakenWhenShielded = 0;
    public bool canShoot = true;
    public bool canMove = true;
    [HideInInspector]
    public float bulletPushBack;
    public float damageReduction, damageMultiplier;
    public Transform pushBackExplosion;
    public bool shouldStopCoroutine = true;
    public bool shouldReplaceActiveCoroutine = true;
    ButtonScript abilityButton;
    public SpriteRenderer shadow;

    [Header ("Movement and aiming")]
    public float baseTurningSpeed = 30f;
    [HideInInspector]
    public bool standardInput = true;
    [HideInInspector]
    public float curMoveSpeed, slowDuration;
    public float moveSpeed = 10f;
    [HideInInspector]
    public Vector2 moveDirection, lastMoveDirection;
    [HideInInspector]
    public float turningSpeed = 30f;
    [HideInInspector]
    public bool shouldTurn = true;
    [HideInInspector]
    public float angle;

    //Shooting   
    [HideInInspector]
    public float shotCounter;
    [SerializeField]
    public GameObject muzzleFlash;

    [Header ("Survival")]
    [SerializeField]
    public GameObject deathExplosion;
    public Transform spawnEffect;
    public Transform nextLevelEffect;

    [SerializeField]
    public float invincibility = 0f;
    [SerializeField]
    public GameObject electricEffect;

    [HideInInspector]
    public GameObject targetEnemy;
    private bool putTargetOnEnemy;
    public DataHolder data;

    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    [Header("Power")]
    public float minHealth;
    public float attackMultiplier, defenseMultiplier, speedMultiplier;
    float lastTakenDamage;
    float startRegenCooldown = 8f;
    public float regenAmount;
    public bool shouldSlowOnShoot = true;
    public bool reducePushBack = false;

    [Header ("Material")]
    public SpriteRenderer mask;
    public Material maskMat;
    public AudioClip playerHitSound;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        data = DataHolder.instance;
        canMove = false;
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>())
        {
            sprite.enabled = false;
        }
        if(data.isTestBuild)
        {
            testBuild = true;
            testWindow = true;
        }
        else
        {
            testBuild = false;
            testWindow = false;
        }
        interactButton.gameObject.SetActive(false);
        material = new Material(material);
        spriteRenderer.material = material;
        maskMat = mask.material;
        gm = GameManager.instance;
        rb = GetComponent<Rigidbody2D>();
        abilityButton = GameObject.Find("Ability Button").GetComponent<ButtonScript>();
        SetupHealthbar();
        controllerOn = data.controllerOn;

        controls = data.controls;

        controls.Gameplay.AttackPower.started += ctx => GameManager.instance.RedirectPower(1);
        controls.Gameplay.DefensePower.started += ctx => GameManager.instance.RedirectPower(2);
        controls.Gameplay.SpeedPower.started += ctx => GameManager.instance.RedirectPower(3);

        controls.Gameplay.Movement.performed += ctx => moveDirection = ctx.ReadValue<Vector2>();
        controls.Gameplay.Movement.canceled += ctx => moveDirection = Vector2.zero;

        controls.Gameplay.ShootingPrimary.performed += ctx => fireButton.buttonPressed = true;
        controls.Gameplay.ShootingPrimary.canceled += ctx => fireButton.buttonPressed = false;

        controls.Gameplay.ShootingSecondary.started += ctx => ShootSecondaryGun();

        controls.Gameplay.Ability.started += ctx => { if (ability) AbilityCooldown.instance.ButtonPress(); };

        controls.Gameplay.Interact.started += ctx => InteractWithObject();

        controls.Gameplay.Pause.started += ctx => GameManager.instance.PauseGame();

        controls.Enable();

        InvokeRepeating("FindClosestEnemy", 0, 0.25f);
    }

    private void OnEnable()
    {
        isAlive = true;
        material.SetFloat("_FlashAmount", 0.0f);
        if(controls != null)
            controls.Enable();
    }
    private void OnDisable()
    {
        controls.Disable();
    }


    void Update()
    {
        if (ability == null)
            abilityButton.gameObject.SetActive(false);
        else
            abilityButton.gameObject.SetActive(true);

        if (canMove)
            PlayerMovement();

        if (standardInput)
        {
            StandardInputUpdate();
        }

        if (abilityUpdateBool)
        {
            ability.UpdateAbility();
        }

        if (invincibility > 0)
        {
            invincibility -= Time.deltaTime;
        }

        if (targetEnemy == null)
            FindClosestEnemy();

        ship.secondaryGun.SecondaryGunUpdate();

        transform.rotation = Quaternion.Lerp(transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), turningSpeed * Time.fixedDeltaTime);
    }

    private void FixedUpdate()
    {
        PlayerMovementFixedUpdate();
        if (movementJoystick.gameObject.activeSelf && !controllerOn)
            moveDirection = Vector2.up * movementJoystick.Vertical + Vector2.right * movementJoystick.Horizontal;
        if(!canMove)
            foreach (Transform thruster in thrusters)
                thruster.gameObject.SetActive(false);
        if (health < maxHealth && (health / maxHealth * 100) < (minHealth / maxHealth * 100) + (regenAmount / maxHealth * 100) && Time.time - lastTakenDamage >= (startRegenCooldown - ship.defenseMultiplier / 10))
        {
            health += defenseMultiplier == 0 ? ship.regenPerSecond * Time.fixedDeltaTime : 3 * ship.regenPerSecond * Time.fixedDeltaTime;
            if (health > maxHealth)
                health = maxHealth;
            healthSlider.value = .125f + health / maxHealth * .875f;
            healthText.text = (Mathf.CeilToInt(health / maxHealth * 100f)).ToString();
        }
        else if(health < maxHealth && ship.constantRegenPerSecond > 0)
        {
            health += ship.constantRegenPerSecond * Time.fixedDeltaTime;
            if (health > maxHealth)
                health = maxHealth;
            healthSlider.value = .125f + health / maxHealth * .875f;
            healthText.text = (Mathf.CeilToInt(health / maxHealth * 100f)).ToString();
        }
    }

    private void LateUpdate()
    {
        if (putTargetOnEnemy && targetEnemy)
            target.position = targetEnemy.transform.position;
    }

    void PlayerMovement()
    {
        if (moveSpeed != maxMoveSpeed)
        {
            if (moveSpeed < maxMoveSpeed)
                electricEffect.SetActive(true);
            slowDuration -= Time.deltaTime;
            if (slowDuration <= 0)
            {
                moveSpeed = maxMoveSpeed;
                electricEffect.SetActive(false);
            }
        }

        if (moveDirection != Vector2.zero)
        {
            foreach (Transform thruster in thrusters)
                thruster.gameObject.SetActive(true);
            lastMoveDirection = moveDirection;
        }
        else
        {
            foreach (Transform thruster in thrusters)
                thruster.gameObject.SetActive(false);
        }
    }

    void PlayerMovementFixedUpdate()
    {
        if (rb.velocity != Vector2.zero)
        {
            curMoveSpeed =  moveSpeed / 2f;
            Vector2 rbvel = rb.velocity;
            rb.velocity -= rbvel * 6.25f * Time.fixedDeltaTime;
            if ((rb.velocity - Vector2.zero).sqrMagnitude <= .01f)
            {
                rb.velocity = Vector2.zero;
                curMoveSpeed = moveSpeed;
            }
        }
        if (moveDirection != Vector2.zero && canMove)
            rb.MovePosition(rb.position + moveDirection * curMoveSpeed * Time.fixedDeltaTime);
    }
    void StandardInputUpdate()
    {
        //targetEnemy = FindClosestEnemy();
        //targetEnemy = 

        if (targetEnemy && (targetEnemy.transform.position - transform.position).magnitude <= attackRange * attackRange)
        {
            SetTarget(targetEnemy.transform);
            putTargetOnEnemy = true;
        }
        else
        {
            shouldTurn = true;
            turningSpeed = baseTurningSpeed;
            CameraMovement.instance.targetEnemy = null;
            target.gameObject.SetActive(false);
            putTargetOnEnemy = false;
        }

        shotCounter -= Time.deltaTime;

 

        if (fireButton.buttonPressed && canShoot)
        {
            isShooting = true;

            if (shotCounter <= 0)
            {
                if (shouldStopCoroutine)
                    StopShootingCoroutine();
                if (shouldReplaceActiveCoroutine)
                    ship.gun.activeCoroutine = StartCoroutine(ship.gun.Shoot());
                else
                    StartCoroutine(ship.gun.Shoot());
            }
        }

        if (isShooting)
        {
            if (shouldSlowOnShoot)
                curMoveSpeed = moveSpeed * .65f;
            else
            {
                curMoveSpeed = moveSpeed * .8f;
            }
        }
        else
        {
            curMoveSpeed = moveSpeed;
        }

        if (!Equals(moveDirection, Vector2.zero) && (!targetEnemy || !isShooting))
        {
            angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90;
        }

    }

    public void SetTarget(Transform targetEnemyTransform)
    {
        Vector3 targetEnemyPos = targetEnemyTransform.position;

        target.gameObject.SetActive(true);
        CameraMovement.instance.targetEnemy = targetEnemyTransform;

        if (isShooting || moveDirection == Vector2.zero)
        {
            angle = Mathf.Atan2(targetEnemyPos.y - transform.position.y, targetEnemyPos.x - transform.position.x) * Mathf.Rad2Deg - 90;
            shouldTurn = false;
            turningSpeed = baseTurningSpeed * 2f;
        }
        else
        {
            shouldTurn = true;
            turningSpeed = baseTurningSpeed;
        }
        //transform.rotation = Quaternion.Euler(0, 0, angle);
        //transform.rotation = Quaternion.Lerp(transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), turningSpeed * Time.fixedDeltaTime);
    }

    public void FindClosestEnemy()
    {
        float distanceToClosestEnemy = (attackRange * attackRange);
        GameObject closestEnemy = null;
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach(GameObject currentEnemy in allEnemies)
        {
            float distanceToEnemy = (currentEnemy.transform.position - transform.position).sqrMagnitude;
            if (distanceToEnemy - distanceToClosestEnemy <= -1f)
            {
                if (!Physics2D.CircleCast(transform.position, .05f, currentEnemy.transform.position - transform.position, (currentEnemy.transform.position - transform.position).magnitude, layermask))
                {
                    distanceToClosestEnemy = distanceToEnemy;
                    closestEnemy = currentEnemy;
                }
            }
        }
        //return closestEnemy;
        targetEnemy = closestEnemy;
    }

    public void SetupHealthbar()
    {
        if(testWindow)
        {
            data = DataHolder.instance;
            data.selectedShip.currentHealth = data.selectedShip.maxHealth;
            data.selectedShip.minHealth = data.selectedShip.maxHealth;
        }
        maxHealth = data.selectedShip.maxHealth;
        health = data.selectedShip.currentHealth;
        minHealth = data.selectedShip.minHealth;
        regenAmount = data.selectedShip.regenAmount;
        healthSlider.value = .125f + health / maxHealth * .875f;
        healthText.text = (Mathf.CeilToInt(health / maxHealth * 100f)).ToString();
    }

    public void InteractWithObject()
    {
        if(interactableObject)
            interactableObject.UnlockCollectible();
    }

    public void ShootSecondaryGun()
    {
        if(canShoot && secondaryFireButton.gameObject.activeSelf)
            StartCoroutine(ship.secondaryGun.Shoot());
    }

    public void TakeDamage(float damage)
    {
        if (invincibility <= 0)
            StartCoroutine(TakeDamageCoroutine(damage));
    }
    IEnumerator TakeDamageCoroutine(float damage)
    {
        float chance = Random.Range(0f, 100f);
        if (chance < ship.chanceToBlockAttack)
        {
            TextMeshPro text = Instantiate(GameManager.instance.immuneText, transform.position, Quaternion.identity).GetComponent<TextMeshPro>();
            text.text = "blocked!";
        }
        else
        {
            damage *= (1 - damageReduction / 100f);
            damage *= (1 - defenseMultiplier / 100f);
            if (canTakeDamage)
            {
                StartCoroutine(CameraShake.instance.Shake(Mathf.Clamp(damage / 10f, 1/12.5f, 100), 1 / 10f));
                SoundManager.instance.soundSource.PlayOneShot(playerHitSound);
                health -= damage;
                data.selectedShip.currentHealth = health;
                lastTakenDamage = Time.time;
                if (health < minHealth)
                    minHealth = health;
                healthSlider.value = .125f + health / maxHealth * .875f;
                healthText.text = (Mathf.CeilToInt(health / maxHealth * 100f) < 0 ? 0 : Mathf.CeilToInt(health / maxHealth * 100f)).ToString();
                invincibility = .3f;
                if (health <= 0)
                {
                    StartCoroutine(KillPlayer());
                }
                material.SetColor("_FlashColor", Color.white);
                material.SetFloat("_FlashAmount", .85f);

                yield return new WaitForSeconds(.15f);

                material.SetFloat("_FlashAmount", 0.0f);
            }
            else
                damageTakenWhenShielded += damage;
        }
    }
    IEnumerator KillPlayer()
    {
        if (SceneManager.GetActiveScene().buildIndex != 1 && SceneManager.GetActiveScene().buildIndex != 2)
        {
            data.gameHasEnded = true;
            data.Save();
            Instantiate(deathExplosion, transform.position, transform.rotation);
            isAlive = false;
            StopShootingCoroutine();
            if (ability != null && ability.isUpdate)
            {
                if (abilityManager.abilityUpdateOn)
                {
                    abilityManager.abilityUpdateOn = false;
                    ability.CancelUpdateAbility();
                    fireButton.buttonPressed = false;
                }
            }
            gm.GoToRespawnMenu();
            gameObject.SetActive(false);
        }
        else if(SceneManager.GetActiveScene().buildIndex == 1)
        {
            Instantiate(deathExplosion, transform.position, transform.rotation);
            StopShootingCoroutine();
            isAlive = false;
            if (ability != null && ability.isUpdate)
            {
                if (abilityManager.abilityUpdateOn)
                {
                    abilityManager.abilityUpdateOn = false;
                    ability.CancelUpdateAbility();
                    fireButton.buttonPressed = false;
                }
            }
            yield return new WaitForSeconds(1f);
            LevelLoader.instance.LoadLevel(SceneManager.GetActiveScene().name);
        }
        else if(SceneManager.GetActiveScene().buildIndex == 2)
        {
            Instantiate(deathExplosion, transform.position, transform.rotation);
            StopShootingCoroutine();
            isAlive = false;
            if (ability != null && ability.isUpdate)
            {
                if (abilityManager.abilityUpdateOn)
                {
                    abilityManager.abilityUpdateOn = false;
                    ability.CancelUpdateAbility();
                    fireButton.buttonPressed = false;
                }
            }

            yield return new WaitForSeconds(.5f);
            TrainingManager.instance.ResetMenuFunction(0);
        }
    }
    public void ShieldRegen()
    {
        StartCoroutine(ShieldRegenC());
    }
    public IEnumerator ShieldRegenC()
    {
        material.SetColor("_FlashColor", new Color(0, .5f, 1, .75f));
        float flash = 0;
        while (flash < .7f)
        {
            flash += 6 * Time.deltaTime;
            material.SetFloat("_FlashAmount", flash);
            yield return null;
        }
        while (flash > 0)
        {
            flash -= 6 * Time.deltaTime;
            material.SetFloat("_FlashAmount", flash);
            yield return null;
        }
    }

    public IEnumerator ShowPlayer()
    {
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>(true))
        {
            sprite.enabled = false;
        }
        yield return new WaitForSeconds(.5f);
        Instantiate(spawnEffect, transform.position + spawnEffect.transform.position, Quaternion.identity);
        yield return new WaitForSeconds(.4f);
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>(true))
        {
            sprite.enabled = true;
        }
        yield return new WaitForSeconds(.5f);
        canMove = true;
    }

    public IEnumerator GoToNextLevel()
    {
        if (!loadingNextLevel)
        {
            loadingNextLevel = true;
            canMove = false;
            foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>(true))
            {
                sprite.enabled = false;
            }
            Instantiate(nextLevelEffect, transform.position + nextLevelEffect.transform.position, Quaternion.identity);


            if (data.subLevel == 5)
            {
                SoundManager.instance.soundSource.Stop();
                int k = 9;
                if (data.levelsPlayed.Length >= 4)
                {
                    //data.levelsPlayed = data.levelsPlayed.Substring(2);

                    yield return new WaitForSeconds(1f);

                    data.levelsPlayed = "";
                    gm.hasWon = true;
                    gm.GoToDeathMenu();

                    yield break;
                }

                k = Random.Range(1, 5);
                while (data.levelsPlayed.IndexOf(k.ToString(), System.StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    k = Random.Range(1, 5);
                }

                /* else
                 {
                     if(int.Parse(data.levelsPlayed.Substring(data.levelsPlayed.Length - 1)) == 2)
                     {
                         //data.hasCompletedStoryMode = true;
                         k = 9;
                     }
                     else
                     {
                         k = int.Parse(data.levelsPlayed.Substring(data.levelsPlayed.Length - 1)) + 1;
                     }
                 }*/
                data.levelsPlayed += k.ToString();
            }

            if (data.subLevel < 5)
                data.subLevel++;
            else
            {
                data.subLevel = 1;
                data.level++;
            }
            data.selectedShip.currentHealth = health;
            data.selectedShip.minHealth = minHealth;
            data.selectedShip.regenAmount = regenAmount;
            canMove = false;
            canShoot = false;
            data.Save();

            yield return new WaitForSeconds(1f);

            if (SceneManager.GetActiveScene().buildIndex != 1)
                StartCoroutine(LevelLoader.instance.LoadLevel(data.levelsPlayed.Substring(data.levelsPlayed.Length - 1)));
            else
                StartCoroutine(LevelLoader.instance.LoadLevel("Main Menu"));
        }
    }

    public void StopShootingCoroutine()
    {
        if (ship.gun.activeCoroutine != null)
        {
            StopCoroutine(ship.gun.activeCoroutine);
            if (ship.gun.isCharging)
            {
                ship.gun.isCharging = false;
                ship.gun.CancelShooting();
            }
        }
    }
}
