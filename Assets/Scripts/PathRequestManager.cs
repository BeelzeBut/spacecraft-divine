using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PathRequestManager : MonoBehaviour
{
    Queue<PathRequest> pathRequestQ = new Queue<PathRequest>();
    PathRequest currentPathRequest;

    static PathRequestManager instance;
    Pathfinding1 pathfinding;
    public float maxQs = 50;
    private float qCounter;

    bool isProcessingPath;
    private void Awake()
    {
        instance = this;
        pathfinding = GetComponent<Pathfinding1>();
    }

    public static void RequestPath(Vector2 pathStart, Vector2 pathEnd, Action<Vector2[], bool> callback)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback);
        instance.pathRequestQ.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    void TryProcessNext()
    {
        if(!isProcessingPath && pathRequestQ.Count > 0)
        {
            currentPathRequest = pathRequestQ.Dequeue();
            isProcessingPath = true;
            StartCoroutine(CancelPathRequest());
            pathfinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
        }

    }

    public void FinishedProcessingPath(Vector2[] path, bool succes)
    {
        currentPathRequest.callback(path, succes);
        isProcessingPath = false;
        TryProcessNext();
        qCounter--;
        StopCoroutine(CancelPathRequest());
    }

    IEnumerator CancelPathRequest()
    {
        yield return new WaitForSeconds(.2f);
        FinishedProcessingPath(null, false);
    }
    struct PathRequest
    {
        public Vector2 pathStart;
        public Vector2 pathEnd;
        public Action<Vector2[], bool> callback;

        public PathRequest(Vector2 start, Vector2 end, Action<Vector2[], bool> _callback)
        {
            pathStart = start;
            pathEnd = end;
            callback = _callback;
        }

    }
}
