using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameManager : MonoBehaviour
{
    DataHolder data;
    void Start()
    {
        data = DataHolder.instance;
        if (data.dataSaved.hasCompletedTutorial)
            StartCoroutine(LoadLevel("Main Menu"));
        else
            StartCoroutine(LoadLevel("Tutorial"));
    }

    public IEnumerator LoadLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        yield return null;
    }
}
