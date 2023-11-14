using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitingBullet : Bullet
{
    public Bullet smallBulletPrefab;
    public float smallBulletDamage = 0;
    public int numberOfSmallBullets;
    public float rotationSpeed;
    public float maxOrbitingRange = 2.5f;
    public float orbitExtensionConstant = 1f;
    public Transform rotativeObject;
    public List<Transform> components;
    public bool startMoving = true;
    private Vector3 initialScale;

    public bool isHoming;
    public bool shouldTurn = true;
    public float turningSpeed = 1.25f;
    public Transform target;
    private void FixedUpdate()
    {
        if (startMoving)
        {
            //rb.MovePosition((Vector2)transform.position + (Vector2)transform.right * speed * Time.fixedDeltaTime);
            transform.position += transform.right * speed * Time.fixedDeltaTime;
            rotativeObject.Rotate(Vector3.forward * rotationSpeed * Time.fixedDeltaTime);
            maxOrbitingRange -= Time.fixedDeltaTime;
            if (maxOrbitingRange > 0)
            {
                for (int i = 0; i < components.Count; i++)
                {
                    if (components[i])
                    {
                        components[i].transform.position += components[i].right * Time.fixedDeltaTime;
                    }
                    else
                    {
                        components.RemoveAt(i);
                    }
                }
            }

            if(isHoming)
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
        }
    }
    public void SpawnSmallBullets()
    {
        Vector3 deviation = Vector3.forward * (180f / numberOfSmallBullets - 180);
        for (int i = 0; i < numberOfSmallBullets; i++)
        {
            Bullet bullet = Instantiate(smallBulletPrefab, transform.position, Quaternion.Euler(transform.rotation.eulerAngles + deviation), rotativeObject);
            bullet.gameObject.layer = gameObject.layer;
            initialScale = bullet.transform.localScale;
            bullet.transform.localPosition += bullet.transform.right * .01f;
            components.Add(bullet.transform);
            deviation += Vector3.forward * 360f / numberOfSmallBullets;
            if (smallBulletDamage > 0)
                bullet.damage = smallBulletDamage;
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
     private void OnDestroy()
     {
         foreach (Transform bullet in components)
         {
            if (bullet)
            {
                bullet.SetParent(null);
                bullet.rotation = transform.rotation;
                bullet.GetComponent<Bullet>().speed = speed;
                bullet.GetComponent<Bullet>().ignoreCollisionLayer = 0;
                bullet.GetComponent<Bullet>().shouldDestroyOnCollision = true;
            }
        }
     }
}
