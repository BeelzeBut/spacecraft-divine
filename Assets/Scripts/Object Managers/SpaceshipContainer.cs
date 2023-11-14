using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipContainer : MonoBehaviour
{
    public static SpaceshipContainer instance;
    public List<Spaceship> allSpaceships = new List<Spaceship>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
}
