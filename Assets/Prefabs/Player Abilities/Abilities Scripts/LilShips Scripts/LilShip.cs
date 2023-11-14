using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LilShip : MonoBehaviour
{
    public PlayerController p;
    public float health;
    public float damage;
    [SerializeField]
    public GameObject explosion;
    public float moveCooldown;
    public float shootCooldown;
    void Start()
    {
        p = PlayerController.instance;
    }

    void Update()
    {
        
    }
}
