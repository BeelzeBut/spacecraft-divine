using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatternBullet : Bullet
{
    public float rotationSpeed;
    [SerializeField]
    public GameObject rotativeObject;
    public List<Bullet> components = new List<Bullet>();

    public bool isHoming;
    public bool isRotative;
    public bool isHelix;

    private bool shouldTurn = true;
    public float stopTargetingTimer = 1f;
    public Transform target;
    public float turningSpeed;

    public Transform[] helixBullets = new Transform[2];
    public float helixRadius = .5f;
    public float helixTimer = 0f;
    public float helixSpeed = 1f;

    private void Start()
    {
        InvokeRepeating("CheckForComponents", 0, 1f);
        if (isComposed)
        {
            Bullet[] bullets = GetComponentsInChildren<Bullet>();
            for(int i = 1; i < bullets.Length; i++)
            { 
                bullets[i].hasPushBack = hasPushBack;
                bullets[i].pushBack = pushBack / Mathf.Clamp(components.Count - 1, 1, 1000);
                bullets[i].damage = damage / components.Count;
                bullets[i].layerMask = layerMask;
                bullets[i].mainBullet = transform;
                bullets[i].latentSpeed = speed;
                bullets[i].canBounce = canBounce;
                bullets[i].canPierce = canPierce;
                bullets[i].canExplode = canExplode;
                bullets[i].whoShotIt = whoShotIt;
            }
        }
        if (isHoming && stopTargetingTimer != 0)
            StopTargeting(stopTargetingTimer);
    }
    private void Update()
    {
        transform.position += transform.right * Time.deltaTime * speed;
        //rb.MovePosition(rb.position + (Vector2)transform.right * speed * Time.fixedDeltaTime);
        PatternBehaviours();
    }

    private void FixedUpdate()
    {
        
    }

    void PatternBehaviours()
    {
        if (isRotative)
            rotativeObject.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        if(isHelix)
        {
            helixTimer += Time.deltaTime * helixSpeed;
            for(int i = 0; i < helixBullets.Length; i++)
            {
                int k = 1;
                if (i == 1)
                    k = -1;
                if(helixBullets[i] && helixBullets[i].parent)
                    helixBullets[i].position = transform.position + transform.up * Mathf.Sin(helixTimer * 10) * helixRadius * k;
            }
        }
        if (isHoming)
            GoToEnemy();
    }
    void GoToEnemy()
    {
        if (target)
        {
            if (shouldTurn)
            {
                Vector3 Target = target.transform.position;

                Vector3 lookDirection = Target - transform.position;
                var angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Slerp(transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), turningSpeed * Time.deltaTime);
            }
        }
    }
    public void StopTargeting(float time)
    {
        StartCoroutine(StopTargetingC(time));
    }
    public IEnumerator StopTargetingC(float time)
    {
        yield return new WaitForSeconds(time);
        shouldTurn = false;
    }
    public void CheckForComponents()
    {
        if(GetComponentsInChildren<Bullet>().Length <= 1)
            Destroy(gameObject);
    }
}
