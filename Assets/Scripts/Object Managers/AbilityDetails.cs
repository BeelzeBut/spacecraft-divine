using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityDetails : MonoBehaviour
{
    public Transform abilityDetails;
    public float openTime = .5f;
    public bool detailsOpen;

    public void OpenCloseDetails()
    {
        StopAllCoroutines();
        if (!detailsOpen)
        {
            StartCoroutine(OpenDetailsC());
            detailsOpen = true;
        }
        else
        {
            StartCoroutine(CloseDetailsC());
            detailsOpen = false;
        }
    }

    IEnumerator OpenDetailsC()
    {      
        while (abilityDetails.localScale.y < 1)
        {
            abilityDetails.localScale += Vector3.up * (1 / openTime) * Time.deltaTime;
            if (abilityDetails.localScale.y > 1)
                abilityDetails.localScale = Vector3.one;
            yield return null;
        }
        abilityDetails.localScale = Vector3.one;
    }
    IEnumerator CloseDetailsC()
    {
        while (abilityDetails.localScale.y > 0)
        {
            abilityDetails.localScale -= Vector3.up * (1 / openTime) * Time.deltaTime;
            if(abilityDetails.localScale.y < 0)
                abilityDetails.localScale = new Vector3(1, 0, 1);
            yield return null;
        }
        abilityDetails.localScale = new Vector3(1, 0, 1);
    }

    private void OnDisable()
    {
        if(detailsOpen)
        {
            detailsOpen = false;
            abilityDetails.localScale = new Vector3(1, 0, 1);
        }
    }
}
