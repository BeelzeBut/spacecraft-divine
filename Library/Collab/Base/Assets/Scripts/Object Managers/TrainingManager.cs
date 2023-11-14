using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrainingManager : MonoBehaviour
{
    public static TrainingManager instance;
    [Header("General")]
    public TextMeshProUGUI title;
    public GameObject mapSelection, roomSizeSelection, roomCenterSelection, enemiesSelection;

    [Header("Map selection")]
    public string mapTitle = "Select a map";
    public Sprite[] mapImages;
    public enum Map{
        crystal,
        swamp,
        moon,
        red
    };
    Map selectedMap;

    [Header("Size Selection")]
    public string sizeTitle = "Select room size";
    public Image[] sizeButtonImages;
    public enum Size { 
        small,
        medium,
        large
    };
    Size selectedSize;
    public GameObject[] crystalRooms = new GameObject[3];
    public GameObject[] swampRooms = new GameObject[3];
    public GameObject[] moonRooms = new GameObject[3];
    public GameObject[] redRooms = new GameObject[3];
    public GameObject[][] rooms;
    public GameObject activeRoom;

    [Header("Center Selection")]
    public string centerTitle = "Select room center";
    public int centerSize;
    public Centers centerPrefabs;
    public GameObject[] crystalSmall;
    public GameObject[] crystalMedium;
    public GameObject[] crystalLarge;
    public GameObject[] swampSmall;
    public GameObject[] swampMedium;
    public GameObject[] swampLarge;
    public GameObject[] moonSmall;
    public GameObject[] moonMedium;
    public GameObject[] moonLarge;
    public GameObject[] redSmall;
    public GameObject[] redMedium;
    public GameObject[] redLarge;
    public GameObject[][][] centers;
    public GameObject activeCenter;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        rooms[0] = crystalRooms;
        rooms[1] = swampRooms;
        rooms[2] = moonRooms;
        rooms[3] = redRooms;

        centers[0][0] = crystalSmall;
        centers[0][1] = crystalMedium;
        centers[0][2] = crystalLarge;
        centers[1][0] = swampSmall;
        centers[1][1] = swampMedium;
        centers[1][2] = swampLarge;
        centers[2][0] = moonSmall;
        centers[2][1] = moonMedium;
        centers[2][2] = moonLarge;
        centers[3][0] = redSmall;
        centers[3][1] = redMedium;
        centers[3][2] = redLarge;
    }

    public void SelectMap(int index)
    {
        selectedMap = (Map)index;
        mapSelection.SetActive(false);
        roomSizeSelection.SetActive(true);
        title.text = "Select room size";
    }

    public void SelectSize(int index)
    {
        foreach(Image img in sizeButtonImages)
        {
            img.sprite = mapImages[(int)selectedMap];
        }
        selectedSize = (Size)index;
        roomSizeSelection.SetActive(false);
        roomCenterSelection.SetActive(true);
        title.text = centerTitle;
    }

    public void SelectCenter()
    {
        
    }
}
