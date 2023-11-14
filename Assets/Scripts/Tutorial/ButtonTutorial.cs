using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;


public class ButtonTutorial : MonoBehaviour
{
    bool canClose = false;
    public Image raycastBlocker;
    DataHolder data;
    private void Start()
    {
        data = DataHolder.instance;
        StartCoroutine(ActivateCloseOption());
        raycastBlocker.enabled = true;
        //data.OnTouch += Touched;
    }

    /*private void OnDisable()
    {
        data.OnTouch -= Touched;
    }

    public void Touched()
    {
        if(canClose)
            StartCoroutine(Close());
    }*/

    private void Update()
    {
        if (canClose)
            if (Input.anyKeyDown || Input.GetMouseButtonDown(1) || Input.touchCount > 0)
                Close();
    }

    void Close()
    {
        DataHolder.instance.hasCompletedButtonsTutorial = true;
        DataHolder.instance.Save();
        raycastBlocker.enabled = false;
        gameObject.SetActive(false);    
    }
    IEnumerator ActivateCloseOption()
    {
        yield return new WaitForSeconds(1f);
        canClose = true;
    }
}
