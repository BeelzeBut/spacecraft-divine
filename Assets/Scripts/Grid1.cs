using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid1 : MonoBehaviour
{
    public static Grid1 instance;
    public bool displayGridGizmos;
    public LayerMask obstacleMask;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    Node[,] grid;
    public bool createNewGrid = false;

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    private void Awake()
    {
        instance = this;

        transform.position = new Vector3(.5f, 0, 0);
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        //CreateGrid();
        //GetComponent<Pathfinding1>().InitializeGridSize();
    }

   /* private void Update()
    {
        if (createNewGrid)
        {
            createNewGrid = false;
            CreateGrid();
        }
    }
    */

    public void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector2 worldBottomLeft = (Vector2)transform.position - Vector2.right * gridWorldSize.x / 2 - Vector2.up * gridWorldSize.y / 2;

        for(int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector2 worldPoint = worldBottomLeft + Vector2.right * (x * nodeDiameter + nodeRadius) + Vector2.up * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics2D.OverlapCircle(worldPoint, nodeRadius, obstacleMask));
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    public int MaxSize
    {
        get
        {
            return gridSizeX * gridSizeY;
        }
    }
    List<Node> neighbours = new List<Node>();

    public List<Node> GetNeighbours(Node node)
    {
        neighbours.Clear();

        for(int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                
                if(checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    public Node NodeFromWorldPoint(Vector2 worldPos)
    {
        float percentX = (worldPos.x + gridWorldSize.x / 2) / gridWorldSize.x;
        percentX = Mathf.Clamp01(percentX);
        float percentY = (worldPos.y + gridWorldSize.y / 2) / gridWorldSize.y;
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector2(gridWorldSize.x, gridWorldSize.y));
        if (grid != null && displayGridGizmos)
        {
            foreach (Node node in grid)
            {
                Color color = node.walkable ? Color.green : Color.red;
                color.a = .35f;
                Gizmos.color = color;
                Gizmos.DrawCube(node.worldPos, Vector3.one * (nodeDiameter));
            }
        }
    }
}
