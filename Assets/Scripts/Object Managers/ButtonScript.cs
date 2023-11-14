using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool buttonPressed;
    public bool onPress;
    public void OnPointerDown(PointerEventData eventData )
    {
        buttonPressed = true;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        buttonPressed = false;
    }


}
