using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageIfHitsWall : MonoBehaviour
{
    public float lifeTime = 1.25f;
    public float damage;
    public float freezeTime = 1f;
    public Color freezeColor = Color.grey;
    public GameObject explosion;
    Enemy enemy;
    void Start()
    {
        enemy = GetComponent<Enemy>();
    }

    // Update is called once per frame
    void Update()
    {
        if (enemy.rb.velocity == Vector2.zero)
        {
            enemy.rb.drag = 5f;
            Destroy(this);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        Instantiate(GameManager.instance.criticalText, transform.position, Quaternion.identity);
        Instantiate(explosion, transform.position, Quaternion.identity).transform.localScale *= 1.5f;
        enemy.Freeze(freezeTime, freezeColor);
        enemy.TakeDamage(damage * (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f));

        enemy.rb.drag = 5f;
        enemy.rb.velocity = Vector2.zero;
        Destroy(this);
    }
}
