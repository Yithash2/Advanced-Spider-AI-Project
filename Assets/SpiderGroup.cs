using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderGroup : MonoBehaviour
{
    [SerializeField] private float refreshSpeed = 0.25f;
    [field:SerializeField] public Rigidbody2D target {get; private set;}
    
    private Vector3[] groupPath;
    private float refreshCounter;
    private List<spider_script> members = new List<spider_script>();
    private Vector3 groupCenter;

    [SerializeField] private float groupCheckSpeed = 1f;
    private float checkCounter;
    
    [SerializeField] private LayerMask layerMask;
    
    [Header("Stagnation Detection")]
    [SerializeField] private float stagnationCheckInterval = 2f;
    private float stagnationCheckCounter = 0f;
    private Vector3 lastRecordedCenter;
    [SerializeField] private float minMovementThreshold = 0.5f;

    void FixedUpdate()
    {
        if (members.Count == 0) return;

        // Update group center
        groupCenter = Vector3.zero;
        var d = 0f;
        foreach (var spider in members)
        {
            groupCenter += spider.transform.position;
        }
        groupCenter /= members.Count;

        // Request path from group center to target
        refreshCounter += Time.fixedDeltaTime;
        if (refreshCounter >= refreshSpeed)
        {
            Debug.Log("refresh counter");
            AStarDemandsScheduler.RequestPath(groupCenter, target.position, OnPathFound);
            refreshCounter = 0;
        }

        if (checkCounter >= groupCheckSpeed)
        {
            Debug.Log("SpiderGroup : Checking for more members.");
            var hits = Physics2D.CircleCastAll(groupCenter, CalculateGroupSize(), Vector2.right, layerMask.value);
            foreach (var hit in hits)
            {
                var r = hit.transform.TryGetComponent(out spider_script sp);
                if (r)
                {
                    var g = sp.IsInGroup(this);
                    if (g == 0)
                    {
                        AddMember(sp);
                    }else if (g == 1)
                    {
                        var grp = sp.GetGroup();
                        if(grp.target == target)
                            Fuze(grp);
                    }
                }
            }
            checkCounter = 0;
        }
        else
        {
            checkCounter +=  Time.fixedDeltaTime;
        }
        
    }

    void OnPathFound(Vector3[] newPath, bool success)
    {
        if (!success || newPath.Length == 0)
            return;

        groupPath = newPath;
        
        // Distribute path to all group members
        foreach (var spider in members)
        {
            spider.SetGroupPath(groupPath);
        }
    }

    public void AddMember(spider_script spider)
    {
        if (!members.Contains(spider))
            members.Add(spider);
        spider.AddGroup(this);
    }

    public void RemoveMember(spider_script spider)
    {
        spider.RemoveGroup();
        members.Remove(spider);

        if (members.Count <= 1)
        {
            members[0].RemoveGroup();
            members.Remove(members[0]);
            
            DestroyImmediate(this.gameObject);
        }
    }

    public void Fuze(SpiderGroup group2)
    {
        foreach (var spider in group2.members)
        {
            spider.RemoveGroup();
            spider.AddGroup(this);
        }
    }

    public Vector3 GetGroupCenter() => groupCenter;
    

    private float CalculateGroupSize()
    {
        var size = 0f;
        foreach (var spider in members)
        {
            size += spider.CohesionRadius;
        }
        size /= members.Count;
        return size;
    }
    
    public int MemberCount => members.Count;
}
