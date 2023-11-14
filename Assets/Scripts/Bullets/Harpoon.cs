using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Harpoon : Bullet
{
    public List<Enemy> impaledEnemies = new List<Enemy>();
    public List<Vector3> positions = new List<Vector3>();
    bool shouldTakeEnemies = true;
    public Transform impactExplosion;

    private void Update()
    {
        if (shouldTakeEnemies)
            for (int i = 0; i < impaledEnemies.Count; i++)
            {
                if(impaledEnemies[i])
                    impaledEnemies[i].rb.MovePosition(transform.position + positions[i]);
            }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.GetComponent<Enemy>().canTakeDamage)
            {
                Instantiate(impactExplosion, other.transform.position, other.transform.rotation);
                Enemy enemy = other.GetComponent<Enemy>();
                //enemy.StopAllCoroutines();
                //StartCoroutine(enemy.StopAttack());
                enemy.Freeze(-1, Color.white);
                enemy.canMove = false;
                enemy.canLook = false;
                impaledEnemies.Add(enemy);
                positions.Add((enemy.transform.position - transform.position));
            }
        }
        else
        {
            StartCoroutine(ExplodeAfterTime());
            GetComponentsInChildren<Collider2D>()[0].enabled = false;
            GetComponentsInChildren<Collider2D>()[1].enabled = false;
        }     
    }

    public IEnumerator ExplodeAfterTime()
    {
        
        yield return new WaitForSeconds(.0375f);
        speed = 0;
        shouldTakeEnemies = false;
        yield return new WaitForSeconds(.4625f);
        explosion.GetComponent<Explosion>().damage = damage * (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f);
        foreach (Enemy enemy in impaledEnemies)
        {
            //enemy.canMove = true;
            //enemy.canLook = true;
            enemy.StartCoroutine(enemy.UnfreezeC(-1));
        }
        Explode();
        Destroy(gameObject);
    }
}
