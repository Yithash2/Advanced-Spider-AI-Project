using System;
using UnityEngine;

public class BoidComponent : MonoBehaviour
{
    [HideInInspector]
    public Group Group;
    
    [Header("SteeringForces")]
    [SerializeField, Range(0,1)] private float separationStrength = 1f;
    [SerializeField, Range(0,1)] private float seekingStrength = 1f;
    [SerializeField, Range(0,1)] private float cohesionStrength = 1f;
    [SerializeField, Range(0,1)] private float alignmentStrength = 1f;

    public bool IsLost;
    [SerializeField] private float lostTimer;
    private float lostCounter;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Vector2 GroupingBehaviour(Vector2 velocity,Action<Vector3> demandAStarPath, out bool CreatedAPath)
    {
        var steering = ComputeFlocking(velocity);
        steering = Vector2.Lerp(velocity.normalized, steering, 0.4f);
        CreatedAPath = false;
        if (IsLost)
        {
            lostCounter +=  Time.fixedDeltaTime;
            if (lostCounter >= lostTimer)
            {
                demandAStarPath.Invoke(Group.GroupCenter);
                CreatedAPath = true;
                return Vector2.zero;;
            }
        }
        else
        {
            lostCounter = 0;
        }
        
        if(Group.CanMove())
            return steering;

        return Vector2.zero;
    }
    
    private Vector2 ComputeFlocking(Vector2 velocity)
    {
        if (Group == null || Group.MemberCount < 2)
            return Vector2.zero;
        
        var steering = Vector2.zero;

        var localGroup = Group.FindLocalGroup(transform.position);
        steering += Group.CalculateCohesion(localGroup.CenterOfGroup,transform) *cohesionStrength;
        steering += Group.CalculateAlignment(localGroup.AverageVelocity,velocity) * alignmentStrength;
        steering += Group.CalculateSeparation(transform) *  separationStrength;
        steering += Group.CalculateSeeking(transform, velocity) * seekingStrength;
        
        return steering.normalized;
    }

    public void ResetLostCounter()
    {
        lostCounter = 0;
    }
}
