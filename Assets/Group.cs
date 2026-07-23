using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Group : MonoBehaviour
{
    [SerializeField] private float refreshSpeed = 0.25f;
    [field:SerializeField] public Rigidbody2D target {get; private set;}
    
    private Vector3[] groupPath;
    private float refreshCounter;
    private List<IBoid> members = new List<IBoid>();
    
    public Vector2 GroupCenter {get; private set;}

    [SerializeField] private float groupCheckSpeed = 1f;
    private float checkCounter;
    
    [SerializeField] private LayerMask layerMask;
    
    [Header("Group")]
    //[SerializeField] private float localGroupDistance; // Distance between the boid and the other that defines a local group
    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float comfortableDistance = 5f;
    [SerializeField] private float outOfGroupDistance = 20f;

    [SerializeField] private float localGroupDistance = 8f;
    

    private Vector3[] path;
    private int targetIndex;
    public Vector2 currentCheckpoint {get; private set;}

    [SerializeField] private Color TeamColor;
    
    [Header("Movement")]
    [SerializeField] private float pauseAmount;
    private float pauseCounter;
    [SerializeField] private float movementAmount;
    private float movementCounter;

    private void Start()
    {
        GroupCenter = Vector2.zero;
        foreach (var s in members)
        {
            GroupCenter += s.Position;
            
        }
        GroupCenter /= members.Count;
    }

    void FixedUpdate()
    {
        if (members.Count == 0) return;
        
        GroupCenter = Vector2.zero;
        float n = 0;
        foreach (var s in members)
        {
            GroupCenter += s.IsLost ? Vector2.zero : (Vector2)s.Position;
            n += s.IsLost ? 0 : 1;
            
        }

        if (n == 0)
        {
            GroupCenter = Vector2.zero;
            foreach (var s in members)
            {
                GroupCenter += (Vector2)s.Position;
            
            }
            GroupCenter /= members.Count;
        }else
            GroupCenter /= n;

        // Request path from group center to target
        refreshCounter += Time.fixedDeltaTime;
        if (refreshCounter >= refreshSpeed)
        {
            //Debug.Log("refresh counter");
            AStarDemandsScheduler.RequestPath(GroupCenter, target.position, OnPathFound);
            refreshCounter = 0;
        }

        if (checkCounter >= groupCheckSpeed)
        {
            foreach (var s in members)
            {
                if (s.IsLost && Vector2.Distance(s.Position, GroupCenter) < 0.7 * members.Count)
                {
                    s.DeclaredFound();
                }
                else if(!s.IsLost && Vector2.Distance(s.Position, GroupCenter) > 0.7 * members.Count)
                {
                    s.DeclaredLost();    
                }
            }
            checkCounter = 0;
        }
        else
        {
            checkCounter +=  Time.fixedDeltaTime;
        }

        if (IsPaused())
        {
            movementCounter = 0;
            pauseCounter += Time.fixedDeltaTime;
        }
        else
        {
            if (IsMoving())
            {
                movementCounter += Time.fixedDeltaTime;
            }
            else
            {
                pauseCounter = 0;
            }
        }
        
        
    }

    public bool IsPaused()
    {
        return pauseCounter < pauseAmount;
    }

    public bool IsMoving()
    {
        return movementCounter < movementAmount;
    }

    public bool CanMove()
    {
        return !IsPaused() && IsMoving();
    }

    void OnPathFound(Vector3[] newPath, bool success)
    {
        if (!success || newPath.Length == 0)
            return;

        path = newPath;
        targetIndex = 1;
        
        StopAllCoroutines();
        StartCoroutine(FollowPath());
        
        
    }

    public void AddMember(spider_script spider)
    {
        if (!members.Contains(spider))
            members.Add(spider);
        spider.AddGroup(this, TeamColor);
    }

    public void Fuze(Group group2)
    {
        foreach (var spider in group2.members)
        {
            spider.RemoveGroup();
            spider.AddGroup(this, TeamColor);
        }
    }

    IEnumerator FollowPath(float precision = 0.2f)
    {
        if (path == null || path.Length < 1)
            yield break;

        currentCheckpoint = path[targetIndex];
        while (true)
        {
            if (Vector2.Distance(GroupCenter, target.position) < 0.3f)
            {
                yield break;
            }

            Vector3 toWaypoint = currentCheckpoint - GroupCenter;
            float distance = toWaypoint.magnitude;

            // Check if waypoint is reached (within threshold OR if we've passed it)
            bool isClose = distance < precision;
            var direction = targetIndex> 0 && path.Length>3 ? (Vector2)path[targetIndex-1] - (Vector2)GroupCenter : Vector2.zero;
            bool isPassed = Vector3.Dot(direction, toWaypoint) < 0 && distance < 1f;

            if (isClose || isPassed)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                    yield break;

                currentCheckpoint = path[targetIndex];
            }
            
            yield return null;
        }
    }

    public Vector2 CalculateCohesion(Vector2 localGroupCenter, Transform spiderTransform)
    {
        
        var cohesionDirection = (localGroupCenter - (Vector2)spiderTransform.position);

        if (cohesionDirection.magnitude < comfortableDistance)
        {
            return Vector2.zero;
        }
        if (cohesionDirection.magnitude > outOfGroupDistance)
        {
            return cohesionDirection.normalized * 500f;
        }
        
        return cohesionDirection.normalized;
    }

    public Vector2 CalculateAlignment(Vector2 averageVelocity, Vector2 velocity)
    {
        var alignement = averageVelocity - velocity;

        return alignement;
    }

    public Vector2 CalculateSeparation(Transform spiderTransform)
    {
        var separationForce = Vector2.zero;

        foreach (var s in members)
        {
            var separationVector = (Vector2)spiderTransform.position - s.Position;
            
            var distance = separationVector.magnitude;

            if (distance < separationRadius)
            {
                if(distance > 0.01f)
                {
                    separationForce += separationVector.normalized / distance;
                }
            }
        }
        
        return separationForce.normalized;
    }

    public Vector2 CalculateSeeking(Transform spiderTransform, Vector2 velocity)
    {
        var dir = (currentCheckpoint - (Vector2)spiderTransform.position).normalized;
        return dir - velocity;
    }

    public int MemberCount => members.Count;

    public void OnDrawGizmos()
    {
        if (path == null) return;
        
        Gizmos.color = TeamColor;
        for (int i = 0; i < path.Length - 1; i++){
            Gizmos.DrawWireSphere(path[i], 1f);
            Gizmos.DrawLine(path[i], path[i + 1]);
        }
    }

    public LocalGroupInfo FindLocalGroup(Vector2 spiderPosition)
    {
	var center = Vector2.zero;
	var avgVel = Vector2.zero;
	var n = 0;
        foreach (var s in members)
        {
            if(s.Position == spiderPosition) continue;

            if (Vector2.Distance(s.Position, spiderPosition) < localGroupDistance)
            {
				avgVel += s.Velocity;
				center += (Vector2)(s.Position);
				n++;
            }
        }
        
        return new LocalGroupInfo(center/n,avgVel/n);
    }
}

public struct LocalGroupInfo{
	public Vector2 CenterOfGroup;
	public Vector2 AverageVelocity;

	public LocalGroupInfo(Vector2 center, Vector2 avgVel){
		CenterOfGroup = center;
		AverageVelocity = avgVel;
	}


}