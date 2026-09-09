using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SpaceshipDivine.Levels;


public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator instance;
    public GameObject layoutRoom;
    public Color startColor, endColor;

    private int distanceToEnd;
    public int initialDistanceToEnd = 5;

    public Transform generatorPoint;

    public enum Direction { up, right, down, left};
    public Direction selectedDirection;

    public float xOffset = 100f;
    public float yOffset = 100f;

    public LayerMask whatIsRoom;

    private GameObject endRoom;
    private List<GameObject> rooms = new List<GameObject>();

    public RoomPrefabs roomPrefabs;
    public Centers centerPrefabs;
    public GameObject[][] centers = new GameObject[3][];

    private List<GameObject> generatedOutlines = new List<GameObject>();
    public bool shouldGenerateMap = true;

    private void Awake()
    {
        instance = this;
    }
    /// <summary>What actually generated the current level. Submitted with a score so the
    /// server can confirm the run was played on the dungeon it claims.</summary>
    public int LastUsedSeed { get; private set; }

    void Start()
    {
        // Seed the LAYOUT only. Enemy AI, drops and combat keep drawing from the global
        // stream, so a run is not fully deterministic - the daily challenge needs everyone on
        // the same dungeon, not in the same fight.
        //
        // LevelSeed.Consume clears the pending seed as it hands it over. That matters because
        // GenerateLevel calls itself when a layout is rejected for being too small: if the seed
        // were still pending on re-entry, the retry would reseed to the same value, generate
        // the same rejected layout, and recurse until the stack gave out.
        int seed = LevelSeed.Consume();
        LastUsedSeed = seed;

        Random.State stateBeforeGeneration = Random.state;
        Random.InitState(seed);
        try
        {
            GenerateLevel();
        }
        finally
        {
            // Restore even if generation throws, so a failed level cannot leave the whole
            // game's randomness pinned to one seed for the rest of the session.
            Random.state = stateBeforeGeneration;
        }
    }

    /// <summary>
    /// The generation itself, re-entered on rejection. Kept separate from Start so the retry
    /// continues drawing from the seeded stream rather than restarting it.
    /// </summary>
    void GenerateLevel()
    {
        if (shouldGenerateMap)
        {
            Benchmark.GenAttempt(); // [benchmark]
            centers[0] = centerPrefabs.S;
            centers[1] = centerPrefabs.M;
            centers[2] = centerPrefabs.L;

            generatorPoint.position = Vector3.zero;
            GameObject firstRoom = Instantiate(layoutRoom, generatorPoint.position, generatorPoint.rotation);
            firstRoom.GetComponent<SpriteRenderer>().color = startColor;
            selectedDirection = (Direction)Random.Range(0, 4);
            Direction firstDirection = selectedDirection;
            MoveGenerationPoint();
            distanceToEnd = Random.Range(initialDistanceToEnd, initialDistanceToEnd + 2);

            for (int i = 0; i < distanceToEnd; i++)
            {
                GameObject newRoom = Instantiate(layoutRoom, generatorPoint.position, generatorPoint.rotation);

                rooms.Add(newRoom);
                if (i + 1 == distanceToEnd)
                {
                    newRoom.GetComponent<SpriteRenderer>().color = endColor;

                    rooms.RemoveAt(rooms.Count - 1);
                    endRoom = newRoom;
                }

                selectedDirection = (Direction)Random.Range(0, 4);
                MoveGenerationPoint();
                while (Physics2D.OverlapCircle(generatorPoint.position, .2f, whatIsRoom))
                {
                    selectedDirection = (Direction)(((int)selectedDirection + 2) % 4);
                    MoveGenerationPoint();
                    Direction wantedDirection = (Direction)Random.Range(0, 4);
                    selectedDirection = wantedDirection;
                    MoveGenerationPoint();
                }

                Destroy(newRoom, 3f);
            }

            if (endRoom.transform.position.y == 0)
            {
                if ((endRoom.transform.position).magnitude < 45)
                {
                    foreach (GameObject room in rooms)
                        Destroy(room);
                    Destroy(endRoom);
                    Destroy(firstRoom);
                    rooms.Clear();
                    Benchmark.GenRejected(); // [benchmark]
                    GenerateLevel();
                    return;
                }
            }
            else if (endRoom.transform.position.x == 0)
            {
                if ((endRoom.transform.position).magnitude < 38)
                {
                    foreach (GameObject room in rooms)
                        Destroy(room);
                    Destroy(endRoom);
                    Destroy(firstRoom);
                    rooms.Clear();
                    Benchmark.GenRejected(); // [benchmark]
                    GenerateLevel();
                    return;
                }
            }
            else
            {
                if ((endRoom.transform.position).magnitude < 29)
                {
                    foreach (GameObject room in rooms)
                        Destroy(room);
                    Destroy(endRoom);
                    Destroy(firstRoom);
                    rooms.Clear();
                    Benchmark.GenRejected(); // [benchmark]
                    GenerateLevel();
                    return;
                }
            }

            Benchmark.GenSucceeded(rooms.Count + 2); // [benchmark]

            //create room outlines
            CreateRoomOutline(Vector3.zero, 0, 0);

            foreach (GameObject room in rooms)
            {
                CreateRoomOutline(room.transform.position, 1, -1);
            }
            if (DataHolder.instance.subLevel != 5)
                CreateRoomOutline(endRoom.transform.position, 0, 0);
            else
                CreateRoomOutline(endRoom.transform.position, 2, 1);

            generatedOutlines[0].GetComponent<Room>().shouldCloseDoors = false;
            generatedOutlines[generatedOutlines.Count - 1].GetComponent<Room>().isLastRoom = true;
        }
        FindObjectOfType<Grid1>().CreateGrid();

    }

    public void MoveGenerationPoint()
    {
        switch (selectedDirection)
        {
            case Direction.up:
                generatorPoint.position += new Vector3(0f, yOffset, 0f);
                break;
            case Direction.down:
                generatorPoint.position += new Vector3(0f, -yOffset, 0f);
                break;
            case Direction.left:
                generatorPoint.position += new Vector3(-xOffset, 0f, 0f);
                break;
            case Direction.right:
                generatorPoint.position += new Vector3(xOffset, 0f, 0f);
                break;
        }
    }
        
    public void CreateRoomOutline(Vector3 roomPosition, int shouldHaveCenter, int size)
    {

        bool roomAbove = Physics2D.OverlapCircle(roomPosition + new Vector3(0, yOffset, 0), .2f, whatIsRoom);
        bool roomBelow = Physics2D.OverlapCircle(roomPosition + new Vector3(0, -yOffset, 0), .2f, whatIsRoom);
        bool roomRight = Physics2D.OverlapCircle(roomPosition + new Vector3(xOffset, 0, 0), .2f, whatIsRoom);
        bool roomLeft = Physics2D.OverlapCircle(roomPosition + new Vector3(-xOffset, 0, 0), .2f, whatIsRoom);


        int directionCount = 0;
        if (roomAbove)
            directionCount++;
        if (roomBelow)
            directionCount++;
        if (roomLeft)
            directionCount++;
        if (roomRight)
            directionCount++;

        if (size == -1)
        {
            size = Random.Range(0, 100);

            if (size <= 10)
                size = 0;
            else if (size <= 40)
                size = 2;
            else size = 1;
        }
        switch(directionCount)
        {
            case 0: Debug.LogError("No rooms");
                break;

            case 1:
                {
                    if (roomAbove)
                    {
                        generatedOutlines.Add(Instantiate(roomPrefabs.U[size], roomPosition, transform.rotation));
                    }

                    if (roomBelow)
                    {
                        generatedOutlines.Add(Instantiate(roomPrefabs.D[size], roomPosition, transform.rotation));
                    }

                    if (roomLeft)
                    {
                        generatedOutlines.Add(Instantiate(roomPrefabs.L[size], roomPosition, transform.rotation));
                    }

                    if (roomRight)
                    {
                        generatedOutlines.Add(Instantiate(roomPrefabs.R[size], roomPosition, transform.rotation));
                    }
                    break;
                }

            case 2:
                {
                    if (roomAbove && roomBelow)
                        generatedOutlines.Add(Instantiate(roomPrefabs.UD[size], roomPosition, transform.rotation));

                    if (roomAbove && roomLeft)
                        generatedOutlines.Add(Instantiate(roomPrefabs.LU[size], roomPosition, transform.rotation));

                    if (roomAbove && roomRight)
                        generatedOutlines.Add(Instantiate(roomPrefabs.RU[size], roomPosition, transform.rotation));

                    if (roomRight && roomLeft)
                        generatedOutlines.Add(Instantiate(roomPrefabs.LR[size], roomPosition, transform.rotation));

                    if (roomBelow && roomRight)
                        generatedOutlines.Add(Instantiate(roomPrefabs.RD[size], roomPosition, transform.rotation));

                    if (roomBelow && roomLeft)
                        generatedOutlines.Add(Instantiate(roomPrefabs.LD[size], roomPosition, transform.rotation));
                    break;
                }

            case 3:
                {
                    if (roomBelow && roomLeft && roomRight)
                        generatedOutlines.Add(Instantiate(roomPrefabs.LRD[size], roomPosition, transform.rotation));

                    if (roomBelow && roomLeft && roomAbove)
                        generatedOutlines.Add(Instantiate(roomPrefabs.LUD[size], roomPosition, transform.rotation));

                    if (roomRight && roomLeft && roomAbove)
                        generatedOutlines.Add(Instantiate(roomPrefabs.LRU[size], roomPosition, transform.rotation));

                    if (roomRight && roomBelow && roomAbove)
                        generatedOutlines.Add(Instantiate(roomPrefabs.RUD[size], roomPosition, transform.rotation));
                    break;
                }

            case 4:
                {
                    if (roomAbove && roomBelow && roomRight && roomLeft)
                        generatedOutlines.Add(Instantiate(roomPrefabs.LRUD[size], roomPosition, transform.rotation));
                    break;
                }
        }

        generatedOutlines[generatedOutlines.Count - 1].GetComponent<Room>().sizeMultiplier = Mathf.Clamp((size + 1) / 2f, .65f, 1.25f);

        if (shouldHaveCenter == 1)
        {
            Instantiate(centers[size][Random.Range(size == 0 ? 0 : 1, centers[size].Length)], roomPosition, Quaternion.identity);
        }
        else if(shouldHaveCenter == 0)
        {
            Instantiate(centers[size][0], roomPosition, Quaternion.identity);
        }
        else
        {
            //Instantiate(centers[size][Random.Range(1, 4)], roomPosition, Quaternion.identity);
        }
    }
    
}

[System.Serializable]
public class RoomPrefabs
{
    public GameObject[] L, R, D, U,
                      LR, LU, LD, RU, RD, UD,
                      LRU, LRD, LUD, RUD,
                      LRUD;
}

[System.Serializable]
public class Centers 
{
    public GameObject[] S, M, L;
}

