using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    DataHolder data;
    public static LevelLoader instance;
    public bool isMenu;
    public GameObject loadingScreen;
    public Slider loadingBar;
    public Image shipImage;
    public bool canGoToNextLevel;
    public bool shouldDisplayAd = true;
    bool isLoadingLevel = false;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        data = DataHolder.instance;
        loadingScreen = GameObject.Find("LoadingScreen");
        loadingBar = GameObject.Find("LoadingBar").GetComponent<Slider>();
        shipImage = GameObject.Find("LoadingShip").GetComponent<Image>();
        loadingScreen.SetActive(false);

        isLoadingLevel = false;
    }

    public IEnumerator LoadLevel(string sceneName)
    {
        shipImage.sprite = DataHolder.instance.selectedShip.shipSprite;
        yield return new WaitForSeconds(.25f);

        if (DataHolder.instance.countToAd != 0)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                if (!isLoadingLevel)
                {
                    isLoadingLevel = true;
                    loadingScreen.SetActive(true);
                    yield return new WaitForSeconds(.25f);

                    AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
                    operation.allowSceneActivation = false;
                    while (operation.progress < .9f)
                    {
                        loadingBar.value = operation.progress + .1f;
                        yield return null;
                    }
                    loadingBar.value = operation.progress + .1f;
                    yield return new WaitForSeconds(.5f);
                    operation.allowSceneActivation = true;
                }
                else yield break;
            }
            else
            {
                GameManager.instance.GoToMenu();
            }
        }
        else
        {
            if (shouldDisplayAd)
            {
                DataHolder.instance.countToAd = 3;
                Monetization.instance.DisplayTempAd();
            }
            SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
