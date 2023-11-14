using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TripleExplosion : MonoBehaviour
{
    [SerializeField]
    public List<GameObject> explosions = new List<GameObject>();
    void Start()
    {
        StartCoroutine(TripleExp());
        Destroy(gameObject, 2f);
    }


    IEnumerator TripleExp()
    {
        foreach (GameObject explosion in explosions)
        {
            yield return new WaitForSeconds(.35f);
            SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.explosionSounds[1]);
            Vector3 offset = transform.position + new Vector3(Random.Range(-.3f, .3f), Random.Range(-.3f, .3f), 0);
            Instantiate(explosion, offset, Quaternion.identity);
        }

        

    }
}
