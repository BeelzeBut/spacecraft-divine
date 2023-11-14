using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public List<AudioClip> generalSounds = new List<AudioClip>();
    public List<AudioClip> shootSounds = new List<AudioClip>();
    public List<AudioClip> abilitySounds = new List<AudioClip>();
    public List<AudioClip> explosionSounds = new List<AudioClip>();
    public List<AudioClip> UISounds = new List<AudioClip>();
    public AudioClip enemyTeleport, chestTeleport;


    public AudioSource soundSource;
    public AudioSource musicSource;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }
}
