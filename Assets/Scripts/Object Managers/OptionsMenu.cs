using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class OptionsMenu : MonoBehaviour
{
    int i = 0;
    public TextMeshProUGUI qualityText;
    [SerializeField]
    public GameObject optionsMenu, mainMenu;
    public void ChangeQuaility()
    {
        qualityText.text = QualitySettings.names[i];
        QualitySettings.SetQualityLevel(i++, true);
        if (i > 5)
            i = 0;
    }

    public void BackButton()
    {
        optionsMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
}
