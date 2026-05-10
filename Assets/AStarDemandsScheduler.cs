using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarDemandsScheduler : MonoBehaviour
{
    public static AStarDemandsScheduler instance;

    [SerializeField] private AStarMap map;
    [SerializeField] private bool drawStealthBlockedNodes = true;
    [SerializeField] private Color stealthBlockedColor = new Color(1f, 0f, 0f, 0.7f);
    [SerializeField] private float stealthBlockedRadius = 0.25f;

    private readonly List<Vector3> stealthBlockedPositions = new List<Vector3>();
    private AStarAgent agent;

    private readonly Queue<PathRequest> requestQueue = new Queue<PathRequest>();
    private bool isProcessing = false;

    public int MaximumNumberOfRequests = 10;

    private PathRequest currentRequest;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        agent = new AStarAgent(this, map);
    }
    
    public static void RequestPath(Vector3 start, Vector3 end, Action<Vector3[], bool> callback)
    {
        if (instance == null)
        {
            Debug.LogError("AStarDemandsScheduler not present in scene.");
            return;
        }
        
        if (instance.requestQueue.Count >= instance.MaximumNumberOfRequests)
        {
            instance.requestQueue.Dequeue();
        }

        instance.requestQueue.Enqueue(new PathRequest(start, end, callback));
        instance.TryProcessNext();
    }
    
    public static void RequestStealthyPath(Vector3 start, Vector3 end, Species ennemie, float distance ,Action<Vector3[], bool> callback)
    {
        if (instance == null)
        {
            Debug.LogError("AStarDemandsScheduler not present in scene.");
            return;
        }

        // Enforce queue limit
        if (instance.requestQueue.Count >= instance.MaximumNumberOfRequests)
        {
            // Drop oldest request (or you can drop newest)
            instance.requestQueue.Dequeue();
        }

        instance.requestQueue.Enqueue(new PathRequest(start, end,ennemie, distance, callback));
        instance.TryProcessNext();
    }
    
    
    private void TryProcessNext()
    {
        if (isProcessing)
            return;

        if (requestQueue.Count == 0)
            return;

        currentRequest = requestQueue.Dequeue();
        isProcessing = true;

        switch (currentRequest.Type)
        {
            case PathType.Normal :
                StartCoroutine(agent.AStar(currentRequest.pathStart, currentRequest.pathEnd, GameManager.Instance.BadPC ? 10 : 1));
                break;
            case PathType.Stealthy :
                Debug.Log("Stealthy");
                ClearStealthDebug();
                StartCoroutine(agent.StealthAStar(currentRequest.pathStart, currentRequest.pathEnd,currentRequest.Species ,currentRequest.distance, GameManager.Instance.BadPC ? 10 : 1));
                break;
        }
    }
    
    public void FinishedProcessingPath(Vector3[] path, bool success)
    {
        try
        {
            currentRequest.callback(path, success);
        }
        catch (Exception e)
        {
            Debug.LogError("Callback threw exception: " + e);
        }

        isProcessing = false;
        TryProcessNext();
    }

    private void ClearStealthDebug()
    {
        stealthBlockedPositions.Clear();
    }

    public void RecordStealthBlockedNode(Vector3 worldPos)
    {
        stealthBlockedPositions.Add(worldPos);
    }

    private void OnDrawGizmos()
    {
        if (!drawStealthBlockedNodes || stealthBlockedPositions.Count == 0)
            return;

        Gizmos.color = stealthBlockedColor;
        foreach (var pos in stealthBlockedPositions)
        {
            Gizmos.DrawWireSphere(pos, stealthBlockedRadius);
        }
    }

    private enum PathType
    {
        Normal, Stealthy
    }
    private struct PathRequest
    {
        public PathType Type;
        public Vector3 pathStart;
        public Vector3 pathEnd;
        public Species Species;
        public float distance;
        public Action<Vector3[], bool> callback;

        public PathRequest(Vector3 start, Vector3 end, Action<Vector3[], bool> callback)
        {
            this.pathStart = start;
            this.pathEnd = end;
            this.callback = callback;
            Type = PathType.Normal;

            distance = 0;
            Species = Species.Blue;
        }
        
        public PathRequest(Vector3 start, Vector3 end,Species ennemie, float distance, Action<Vector3[], bool> callback)
        {
            this.pathStart = start;
            this.pathEnd = end;
            this.callback = callback;
            this.Species = ennemie;
            this.distance = distance;
            Type = PathType.Stealthy;
        }
    }
}
