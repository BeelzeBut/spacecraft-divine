using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoomerangBullet : Bullet
{
    [SerializeField]
    private GameObject sprite;
    public float rotationSpeed;
    public bool reachedTarget;
    public Vector3 targetPos, initialPos;
    public float turningSpeed;
    public Transform firePoint;
    private float elapsed;
    float distanceToTarget = .05f;
    float timeSinceItReachedTarget = 0f;

    private void Start()
    {
        turningSpeed = 1 / (targetPos - initialPos).magnitude + 2;
    }

    void FixedUpdate()
    {
        sprite.transform.Rotate(Vector3.forward * rotationSpeed * Time.fixedDeltaTime);

        speed = Mathf.Clamp((transform.position - targetPos).magnitude + 1f, 2.25f, 5.5f);
        elapsed += Time.fixedDeltaTime;
        if (!reachedTarget && elapsed > 3.5f)
        {
            reachedTarget = true;
            turningSpeed *= 1.1f;
        }

        if (!reachedTarget)
        {
            if ((transform.position - targetPos).sqrMagnitude <= distanceToTarget)
            {
                turningSpeed *= 1.1f;
                reachedTarget = true;
            }
            Vector3 lookDir = targetPos - transform.position;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), turningSpeed * Time.fixedDeltaTime);
            distanceToTarget += .1f * Time.deltaTime;
        }
        else
        {
            if (whoShotIt)
            {
                timeSinceItReachedTarget += Time.deltaTime;
                if ((transform.position - firePoint.position).sqrMagnitude <= .05f)
                {
                    Destroy(gameObject);
                }
                if (timeSinceItReachedTarget < .75f)
                    turningSpeed += Time.fixedDeltaTime * .75f;
                else
                    turningSpeed += Time.fixedDeltaTime * 20f; 
                Vector3 lookDir = firePoint.position - transform.position;
                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Lerp(transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), turningSpeed * Time.fixedDeltaTime);
            }
        }

        rb.MovePosition(rb.position + (Vector2)(transform.right) * speed * Time.fixedDeltaTime);
    }
}
