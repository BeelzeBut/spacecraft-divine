using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class AbilitiesButton : MonoBehaviour, IPointerDownHandler
{
    AbilityCooldown abilityManager;

    private void Start()
    {
        abilityManager = AbilityCooldown.instance;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        abilityManager.ButtonPress();
    }
}
    
    
