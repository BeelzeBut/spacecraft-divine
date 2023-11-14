using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class Pathfinding1 : MonoBehaviour
{

    PathRequestManager requestManager;
    Grid1 grid;

    private HashSet<Node> closedSet = new HashSet<Node>();
    private Heap<Node> openSet;
    List<Vector2> waypoints = new List<Vector2>();
    List<Node> path = new List<Node>();
    public int maxGridSize;

    Stopwatch sw = new Stopwatch();
    private void Awake()
    {
        requestManager = GetComponent<PathRequestManager>();
        grid = GetComponent<Grid1>();
    }

    public void InitializeGridSize()
    {
        openSet = new Heap<Node>(grid.MaxSize);
    }

    public void StartFindPath(Vector2 startPos, Vector2 targetPos)
    {
        StartCoroutine(FindPath(startPos, targetPos));
    }

    IEnumerator FindPath(Vector2 startPos, Vector2 targetPos)
    {
        Vector2[] waypoints = new Vector2[0];
        bool pathSuccess = false;

        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        if (!targetNode.walkable)
        {
            //Debug.Log("Target node not walkable");
            foreach (Node neighbour in grid.GetNeighbours(targetNode))
            {
                if (neighbour.walkable)
                {
                    targetNode = neighbour;
                    break;
                }
            }
        }
        if (!startNode.walkable)
        {
            //Debug.Log("Starting node not walkable!");
            foreach (Node neighbour in grid.GetNeighbours(startNode))
            {
                if (neighbour.walkable)
                {
                    startNode = neighbour;
                    break;
                }
            }
        }

        if (targetNode.walkable)
        {
            Heap<Node> openSet = new Heap<Node>(grid.MaxSize);

            closedSet.Clear();

            openSet.Add(startNode);
            while (openSet.Count > 0)
            {
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                if (currentNode == targetNode)
                {
                    pathSuccess = true;
                    break;
                }

                foreach (Node neighbour in grid.GetNeighbours(currentNode))
                {
                    if (!neighbour.walkable || closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    int newMoveCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                    if (newMoveCost < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMoveCost;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
                        {
                            openSet.Add(neighbour);
                        }
                    }
                }
            }
        }
        yield return null;
        
        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
        }
        requestManager.FinishedProcessingPath(waypoints, pathSuccess);
    }

    Vector2[] RetracePath(Node startNode, Node endNode)
    {
        //List<Node> path = new List<Node>();
        path.Clear();
        Node currentNode = endNode;

        while(currentNode != startNode)
        {
            if (!path.Contains(currentNode))
            {
                path.Add(currentNode);
                if (currentNode.parent != null)
                    currentNode = currentNode.parent;
                else
                    break;
            }
            else
            {
                //UnityEngine.Debug.Log("circular");
                break;
            }
        }

        Vector2[] waypoints = SimplifyPath(path);
        Array.Reverse(waypoints);

        return waypoints;        
    }

    Vector2[] SimplifyPath(List<Node> path)
    {
        //List<Vector2> waypoints = new List<Vector2>();
        waypoints.Clear();
        Vector2 directionOld = Vector2.zero;
        if (path.Count == 1)
        {
            waypoints.Add(path[0].worldPos);
            return waypoints.ToArray();
        }

        for (int i = 1; i < path.Count - 1; i++)
        {
            //Vector2 directionNew = new Vector2(path[i-1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
            Vector2 directionNew = new Vector2(path[i].gridX - path[i + 1].gridX, path[i].gridY - path[i + 1].gridY);
            if(directionNew != directionOld)
            {
                //waypoints.Add(path[i-1].worldPos);
                waypoints.Add(path[i].worldPos);
            }
            directionOld = directionNew;
        }
        if (path.Count > 1)
            waypoints.Add(path[path.Count - 1].worldPos);
        return waypoints.ToArray();
    }
    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }
}
