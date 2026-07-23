using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class spider_script : MonoBehaviour,IBoid
{
    public Species specie;
    
    [SerializeField] private float speed = 3f;

    [SerializeField] private BoidComponent GroupingComponent;
    public bool IsLost => GroupingComponent.IsLost;
    public Vector2 Position => transform.position;
    
    public Group CurrentGoup
    {
        get => GroupingComponent.Group;
        set => GroupingComponent.Group = value;
    }
    
    [SerializeField] private Group group;
    
    [Header("Pathfinding")]
    [SerializeField] private Species ennemie;
    [SerializeField] private Rigidbody2D target;
    [SerializeField] private float refreshSpeed = 0.25f;
    
    [Header("Behaviour")]
    [SerializeField] private float distanceToTarget;
    [SerializeField] private Behaviour behaviour;
    enum Behaviour
    {
        Attack, Avoid
    }

    [Header("Debug")]
    [SerializeField] private Color pathColor = Color.green;

    [SerializeField]
    private Rigidbody2D rigidBody;
    [field:SerializeField] public ColorChanger ColorComponent { get; private set; }
    private Vector3[] path;
    private int targetIndex;
    private float refreshCounter;
    
    public Vector2 Velocity => rigidBody.linearVelocity;
    

    [SerializeField] private LegsCoordinator legsCoordinator;
    [SerializeField] private Animator animator;



    [SerializeField] private LayerMask temp_layerMask;
    private bool IsCharging = false;
    
    void Start()
    {
        speed += Random.Range(-speed/4, speed/4);
        
        // If part of a group, register with it
        if (group)
        {
            CurrentGoup = group;
            
            CurrentGoup.AddMember(this);
            target = CurrentGoup.target;
        }

        GameManager.Instance.AddSpider(this);
    }

    private void Update()
    {
        legsCoordinator.UpdateAnimation(speed);
    }

    private void OnValidate()
    {
        if (!GroupingComponent)
        {
            GroupingComponent = GetComponent<BoidComponent>();
            CurrentGoup = group;
        }
        
        if(!rigidBody)
            rigidBody = GetComponent<Rigidbody2D>();
    }
    void FixedUpdate()
    {

        if (Mouse.current.leftButton.isPressed)
        {
            //rigidBody.linearVelocity = Vector2.zero;
            animator.SetBool("Emoting", true);
            //return;
        }
        else
        {
            animator.SetBool("Emoting", false);
        }

        if (!IsCharging)
        {
            refreshCounter += Time.fixedDeltaTime;
            if (CurrentGoup == null)
            {
                SoloBehaviour();
            }
            else
            {
                bool createdAPath = false;
                var newVel = GroupingComponent.GroupingBehaviour(rigidBody.linearVelocity, DemandAstarPath,out createdAPath);
                if(!createdAPath)
                    ApplySteering(newVel);
            }

            if(behaviour == Behaviour.Attack)
                if (Vector2.Distance(target.position, transform.position) < distanceToTarget)
                {
                    var dirToTarget = (target.transform.position - transform.position).normalized;
                    var r =Physics2D.Raycast(transform.position, dirToTarget, distanceToTarget, temp_layerMask.value);
                    if (!r)
                    {
                        IsCharging = true;
                        rigidBody.linearVelocity = Vector2.zero;
                        StartCoroutine(Charging(dirToTarget));
                    }
                
                }
        }
        
    }

    IEnumerator Charging(Vector2 direction)
    {
        var origin = transform.position;
        for (float t = 0; t < 1; t+= Time.fixedDeltaTime)
        {
            rigidBody.linearVelocity = -direction.normalized * (speed/3);
            yield return new WaitForFixedUpdate();
        }

        var originalDir = direction;
        direction = (target.transform.position - transform.position).normalized;
        rigidBody.linearVelocity = Vector2.zero;
        if (Mathf.Acos(Vector2.Dot(direction.normalized, originalDir.normalized)) > Mathf.PI/6)
        {
            IsCharging = false;
            yield break;
        }

        var targetDist = (transform.position - (origin)).magnitude + (distanceToTarget * 2);
        for (float t = 0; t < targetDist/(speed*4); t+= Time.fixedDeltaTime)
        {
            rigidBody.linearVelocity = direction.normalized * (speed*4);
            yield return new WaitForFixedUpdate();
        }
        rigidBody.linearVelocity = Vector2.zero;
        var f = Random.Range(0, 2);
        for (float t = 0; t < f; t += Time.fixedDeltaTime)
        {
            yield return new WaitForFixedUpdate();
        }
        
        IsCharging = false;
        
    }

    

    private void SoloBehaviour()
    {
        if(!target) return;
        switch (behaviour)
        {
            case Behaviour.Attack:
                DemandAstarPath(target.position + target.linearVelocity * (Time.deltaTime * refreshSpeed));
                break;
            case Behaviour.Avoid:
                DemandAvoidAStarPath();
                break;
        }
           
        
    }

    private void DemandAstarPath(Vector3 targetPosition)
    {
        if (refreshCounter >= refreshSpeed)
        {
            if (Vector2.Distance(transform.position, target.position) < 1f)
            {
                rigidBody.linearVelocity = Vector2.zero;
            }
            else
                AStarDemandsScheduler.RequestPath(transform.position, targetPosition, OnPathFound);
            
            refreshCounter = 0;
        }
        
    }

    private void DemandAvoidAStarPath()
    {
        if (refreshCounter >= refreshSpeed)
        {
            Vector3 targetPosition = target.position;

            if (Vector2.Distance(transform.position, target.position) < 1f)
            {
                rigidBody.linearVelocity = Vector2.zero;
            }
            else
                AStarDemandsScheduler.RequestStealthyPath(
                    transform.position, targetPosition, ennemie, distanceToTarget, OnPathFound);
            
            refreshCounter = 0;
        }
        
    }
    
    public void Blush(bool b, int legId)
    {
        ColorComponent.Blush(b, legId);
    }

    [SerializeField] private Collider2D[] legsColliders;

    public bool IsMyOwnLeg(Collider2D leg)
    {
        foreach (var legCollider in legsColliders)
        {
            if (legCollider == leg) return true;
        }

        return false;
    }
    
    public void AddGroup(Group group2, Color teamColor)
    {
        path = null;
        CurrentGoup = group2;
        ColorComponent.ChangeColor(teamColor);
        GroupingComponent.Group = CurrentGoup;
        target = group2.target;
    }
    public void DeclaredFound()
    {
        GroupingComponent.IsLost =  false;
        StopCoroutine(FollowPath());
        path = null;
        GroupingComponent.ResetLostCounter();
    }

    public void DeclaredLost()
    {
        GroupingComponent.IsLost = true;
        //lostCounter = 0;
    }
    public void RemoveGroup()
    {
        target = CurrentGoup.target;
        CurrentGoup = null;
        //GroupingComponent.Group = null;
    }
    public void OnPathFound(Vector3[] newPath, bool success)
    {
        if (!success || newPath.Length == 0)
            return;

        path = newPath;
        targetIndex = 0;

        StopCoroutine(FollowPath());
        StartCoroutine(FollowPath());
    }
    public int IsInGroup(Group group2)
    {
        if (CurrentGoup == group2)
            return 2;
        
        if (CurrentGoup == null) 
            return 0;
        
        return 1;
    }
    IEnumerator FollowPath(float precision = 0.2f) {
        if (path == null || path.Length < 1) yield break;
        Vector3 currentWaypoint = path[0];
        while (true) { 
            if(target == null || path == null)
                yield break;
            if (Vector2.Distance(transform.position, target.position) < 0.3f)
            {
                rigidBody.linearVelocity = Vector2.zero; yield break;
            }
            Vector3 toWaypoint = currentWaypoint - transform.position;
            float distance = toWaypoint.magnitude;
            
            toWaypoint = Vector2.Lerp(Velocity.normalized, toWaypoint, 0.125f);
            ApplySteering(toWaypoint.normalized);
            
            bool isClose = distance < precision;
            bool isPassed = Vector3.Dot(rigidBody.linearVelocity, toWaypoint) < 0 && distance < 1f;
            
            if (isPassed || isClose)
            {
                targetIndex++; if (targetIndex >= path.Length) 
                    yield break;
                currentWaypoint = path[targetIndex];
            } 
            yield return new WaitForFixedUpdate(); 
        }
    }
    
    void ApplySteering(Vector2 steering)
    {
        Vector2 desiredVelocity = steering.normalized * speed;
        rigidBody.linearVelocity = desiredVelocity;
        
        rigidBody.linearVelocity = Vector2.ClampMagnitude(rigidBody.linearVelocity, speed);
    }
    private void OnDrawGizmos()
    {
        if (path == null) return;
        
        Gizmos.color = pathColor;
        for (int i = 0; i < path.Length - 1; i++){
            Gizmos.DrawWireSphere(path[i], 1f);
            Gizmos.DrawLine(path[i], path[i + 1]);
        }
    }
}

public enum Species
{
    Blue, Redo
}
