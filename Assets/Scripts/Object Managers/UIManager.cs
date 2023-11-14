using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    GameManager gm;
    public GameObject feedbackForm;
    public Image charge;
    public Image checkboxX;
    [SerializeField]
    public GameObject controllerLayout;
    public Animator lowerRight;
    public TextMeshProUGUI gemsText;
    public Image[] upgrades;
    public Sprite attackSprite, defenseSprite, speedSprite;
    public Image musicImage, soundImage;
    public Sprite musicSprite, musicMuteSprite, soundSprite, soundMuteSprite;
    public Image raycastBlocker;
    public GameObject bossName;

    [Header("Options Menu")]
    public GameObject settingsMenu;
    public TextMeshProUGUI qualityText;
    public Slider cameraSpeedSlider;

    [Header("Death Menu")]
    public TextMeshProUGUI totalGems;
    public TextMeshProUGUI gameOverText;
    public GameObject playerWon;

    private void Awake()
    {
        instance = this;
        gm = GameManager.instance;    
    }
    private void Start()
    {
        checkboxX.enabled = DataHolder.instance.controllerOn;
        if(DataHolder.instance.controllerOn)
            lowerRight.SetTrigger("controlleron");
        else
            lowerRight.SetTrigger("controlleroff");
        qualityText.text = QualitySettings.names[DataHolder.instance.qualityIndex];
        DataHolder.instance.qualityText = qualityText;

        if (DataHolder.instance.isMusicMuted)
            musicImage.sprite = musicMuteSprite;

        if (DataHolder.instance.isSoundMuted)
            soundImage.sprite = soundMuteSprite;
    }
    public void OpenFeedbackForm()
    {
        feedbackForm.SetActive(true);

    }
    public void GamepadControls()
    {
        DataHolder data = DataHolder.instance;
        if (data.controllerOn)
        {
            data.controllerOn = false;
            PlayerController.instance.controllerOn = false;
            checkboxX.enabled = false;
            lowerRight.SetTrigger("controlleroff");
        }
        else
        {
            PlayerController.instance.controllerOn = true;
            data.controllerOn = true;
            checkboxX.enabled = true;
            lowerRight.SetTrigger("controlleron");
        }
    }

    public void OpenControllerLayout()
    {
        controllerLayout.SetActive(true);
    }

    public void CloseControllerLAyout()
    {
        controllerLayout.SetActive(false);
    }
    public void GoToMenu()
    {
        DataHolder data = DataHolder.instance;
        data.gems += (int)(data.goldCoins * data.coinsMultiplier);
        data.goldCoins = 0;
        data.dataSaved.totalEnemiesKilled += data.enemiesKilled;
        data.enemiesKilled = 0;
        data.gameHasEnded = true;
        data.countToAd--;
        data.Save();

        StartCoroutine(LevelLoader.instance.LoadLevel("Main Menu"));
        Time.timeScale = 1;
    }

    public void ChangeQuality()
    {
        DataHolder.instance.ChangeQuality();
    }

    public void FpsSettings()
    {
        DataHolder.instance.FpsSettings();
    }

    public void OptionsMenu()
    {
        settingsMenu.SetActive(true);
        qualityText.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
        PlayerController.instance.controls.Disable();
    }
    IEnumerator CloseOptionsMenu()
    {
        settingsMenu.GetComponent<Animator>().SetTrigger("close");
        yield return new WaitForSecondsRealtime(settingsMenu.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + .05f);
        settingsMenu.SetActive(false);
        PlayerController.instance.controls.Enable();
    }

    public void ChangeCameraSpeed()
    {
        DataHolder.instance.cameraSpeedLevel = (int)cameraSpeedSlider.value;
        CameraMovement.instance.cameraSpeedLevel = 6 - DataHolder.instance.cameraSpeedLevel;
    }
    public void CloseOptions()
    {
        StartCoroutine(CloseOptionsMenu());
    }

    public void UpdateUpgrades()
    {
        int k = 0;
        foreach(Upgrade upgrade in PlayerController.instance.ship.playerUpgrades)
        {
            upgrades[k].GetComponentsInChildren<Image>()[1].sprite = upgrade.isAttack ? attackSprite : upgrade.isDefense ? defenseSprite : speedSprite;
            upgrades[k].GetComponentsInChildren<Image>()[1].color = Color.white;
            k++;
        }
    }

    public void MusicOnOff()
    {
        SoundManager sound = SoundManager.instance;
        if (sound.musicSource.mute)
        {
            sound.musicSource.mute = false;
            musicImage.sprite = musicSprite;
        }
        else
        {
            sound.musicSource.mute = true;
            musicImage.sprite = musicMuteSprite;

        }
    }

    public void SoundOnOff()
    {
        SoundManager sound = SoundManager.instance;
        if (sound.soundSource.mute)
        {
            sound.soundSource.mute = false;
            soundImage.sprite = soundSprite;
            DataHolder.instance.isSoundMuted = false;
        }
        else
        {
            sound.soundSource.mute = true;
            soundImage.sprite = soundMuteSprite;
            DataHolder.instance.isSoundMuted = true;
        }
    }
}
