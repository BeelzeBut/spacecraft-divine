using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;
//using UnityEngine.Monetization;

public class Monetization : MonoBehaviour
{
    //public static Monetization instance;
    //public string googlePlayId = "4142687", iosId = "4142686";
    //string myPlacementId = "rewardedVideo";
    //bool testMode = true;
    //public int rewardedAction = 0;

    //private void Awake()
    //{
    //    instance = this;
    //}
    //void Start()
    //{
    //    Advertisement.AddListener(this);
    //    Advertisement.Initialize(googlePlayId, testMode);
    //}

    //public void DisplayTempAd()
    //{
    //    if(Advertisement.IsReady())
    //        Advertisement.Show();
    //}

    //public void DisplayRewardedAd()
    //{
    //    while (!Advertisement.IsReady())
    //        continue;
    //    Advertisement.Show(myPlacementId);
    //}

    //// Implement IUnityAdsListener interface methods:
    //public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
    //{
    //    // Define conditional logic for each ad completion status:
    //    if (showResult == ShowResult.Finished)
    //    {
    //        switch (rewardedAction)
    //        {
    //            case 0:
    //                DataHolder.instance.gems += DataHolder.instance.videoRewardGems;
    //                MainMenu.instance.RefreshGems();
    //                break;
    //            case 1:
    //                GameManager.instance.RevivePlayer();
    //                Debug.Log("Revived Player");
    //                break;
    //        }
    //    }
    //    else if (showResult == ShowResult.Skipped)
    //    {
    //        Debug.Log("You dont get a reward!");
    //    }
    //    else if (showResult == ShowResult.Failed)
    //    {
    //        Debug.LogWarning("The ad did not finish due to an error.");
    //    }
    //}

    //public void OnUnityAdsReady(string placementId)
    //{
    //    // If the ready Placement is rewarded, show the ad:
    //    if (myPlacementId == placementId)
    //    {

    //    }
    //}

    //public void OnUnityAdsDidError(string message)
    //{
    //    // Log the error.
    //}

    //public void OnUnityAdsDidStart(string placementId)
    //{
    //    // Optional actions to take when the end-users triggers an ad.
    //}

    //// When the object that subscribes to ad events is destroyed, remove the listener:
    //public void OnDestroy()
    //{
    //    Advertisement.RemoveListener(this);
    //}
}
