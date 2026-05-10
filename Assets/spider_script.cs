using System.Collections;
using UnityEngine;

public class spider_script : MonoBehaviour
{
    public Species specie;

    [Header("Steering")]
    [SerializeField] private float[] pattern;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float steeringSmooth = 0.15f;
    [SerializeField] private float rayDistance = 4f;
    [SerializeField] private LayerMask obstacleMask;
    
    [Header("Obstacle Avoidance Forces")]
    [SerializeField] private float avoidanceStrength = 2f;
    [SerializeField, Range(0.000001f, 2)] private float avoidanceFalloff = 1.5f;

    [Header("Flocking (Group Behavior)")]
    [field:SerializeField] public float CohesionRadius { get; private set; }
    [SerializeField] private float cohesionStrength = 0.5f;
    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float separationStrength = 1f;
    [SerializeField] private SpiderGroup group;
    
    [SerializeField] private float separationTime = 1f;
    private float _separationCounter = 0f;
    
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

    private Rigidbody2D rb;
    private Vector3[] path;
    private int targetIndex;
    private float refreshCounter;

    private const int RayCount = 16;
    private Vector2[] rayDirections;
    private Vector2 smoothedSteering;
    

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        PrecomputeRayDirections();
        
        Vector3 targetPosition = target.position +target.linearVelocity * (Time.deltaTime);
        

        // If part of a group, register with it
        if (group != null)
        {
            group.AddMember(this);
        }
        else
        {
            // Solo spider: request own path
            /*switch (behaviour)
            {
                case Behaviour.Attack:
                    AStarDemandsScheduler.RequestPath(transform.position, targetPosition , OnPathFound);
                    break;
                case Behaviour.Avoid:
                    AStarDemandsScheduler.RequestStealthyPath(transform.position, targetPosition ,ennemie, 13, OnPathFound);
                    break;
            }*/
        }

        GameManager.Instance.AddSpider(this);
    }

    void FixedUpdate()
    {
        Vector3 targetPosition = target.position +target.linearVelocity * (Time.deltaTime * refreshSpeed) ;
        
        
        
        refreshCounter += Time.fixedDeltaTime;
        if (refreshCounter >= refreshSpeed)
        {;
            if (group == null)
            {
                switch (behaviour)
                {
                    case Behaviour.Attack:
                        AStarDemandsScheduler.RequestPath(transform.position, targetPosition , OnPathFound);
                        break;
                    case Behaviour.Avoid:
                        AStarDemandsScheduler.RequestStealthyPath(transform.position + (Vector3)rb.linearVelocity * (Time.deltaTime * refreshSpeed), targetPosition ,ennemie, 5, OnPathFound);
                        break;
                }
               
            }
            refreshCounter = 0;
        }

        if (group != null)
        {
            if(Vector2.Distance(transform.position, group.GetGroupCenter())> CohesionRadius)
            {
                _separationCounter += Time.fixedDeltaTime;
                if (_separationCounter >= separationTime)
                    group.RemoveMember(this);
            }
            else
            {
                _separationCounter = 0;
            }
        }
    }

    public void AddGroup(SpiderGroup group2)
    {
        group = group2;
        target = null;
    }

    public SpiderGroup GetGroup()
    {
        return group;
    }

    public void RemoveGroup()
    {
        target = group.target;
        group = null;
    }

    void PrecomputeRayDirections()
    {
        rayDirections = new Vector2[RayCount];
        float angleStep = 360f / RayCount;

        for (int i = 0; i < RayCount; i++)
        {
            float angle = Mathf.Deg2Rad * (i * angleStep);
            rayDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }

    public void OnPathFound(Vector3[] newPath, bool success)
    {
        if (!success || newPath.Length == 0)
            return;

        path = newPath;
        targetIndex = 0;

        StopAllCoroutines();
        StartCoroutine(FollowPath());
    }

    public void SetGroupPath(Vector3[] groupPath)
    {
        path = groupPath;
        targetIndex = 0;

        StopAllCoroutines();
        StartCoroutine(FollowPath( 3f));
    }

    public int IsInGroup(SpiderGroup group2)
    {
        if (group == group2)
            return 2;
        
        if (group == null) 
            return 0;
        
        return 1;
    }
    

    IEnumerator FollowPath(float precision = 3f)
    {
        if (path == null || path.Length < 1)
            yield break;

        Vector3 currentWaypoint = path[0];
        
        while (true)
        {
            Vector3 toWaypoint = currentWaypoint - (group != null ? group.GetGroupCenter() : transform.position);
            float distance = toWaypoint.magnitude;
            
            // Check if waypoint is reached (within threshold OR if we've passed it)
            bool isClose = distance < precision;
            bool isPassed = Vector3.Dot(rb.linearVelocity, toWaypoint) < 0 && distance < 1f;
            
            if (isClose || isPassed)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                    yield break;

                currentWaypoint = path[targetIndex];
            }

            Vector2 steering = ComputeSteering(currentWaypoint);

            /*if (group != null && steering.magnitude < 3)
            {
                group.RemoveMember(this);
            }*/
            
            ApplySteering(steering);

            yield return null;
        }
    }
    
    Vector2 ComputeObstacleAvoidance()
    {
        Vector2 selfPos = transform.position;
        Vector2 avoidance = Vector2.zero;

        for (int i = 0; i < RayCount; i++)
        {
            Vector2 dir = rayDirections[i];

            var hit = Physics2D.RaycastAll(selfPos, dir, rayDistance, obstacleMask);
            if (hit.Length > 1)
            {
                float dist = hit[1].distance;
                float strength = Mathf.Clamp01(1f - (dist / rayDistance * avoidanceFalloff));

                // Repulsion direction = away from obstacle
                Vector2 push = -dir * strength * avoidanceStrength;

                avoidance += push;

                Debug.DrawLine(selfPos, hit[1].point, Color.yellow);
            }
        }

        return avoidance;
    }

    Vector2 ComputeFlocking()
    {
        if (group == null || group.MemberCount < 2)
            return Vector2.zero;

        Vector2 selfPos = transform.position;
        Vector2 flocking = Vector2.zero;

        // Cohesion: move toward group center
        Vector3 groupCenter = group.GetGroupCenter();
        float distToCenter = Vector2.Distance(selfPos, groupCenter);
        if (distToCenter > 0.1f && distToCenter < CohesionRadius)
        {
            Vector2 toCohesion = ((Vector2)groupCenter - selfPos).normalized;
            flocking += toCohesion * cohesionStrength;
        }

        // Separation: avoid crowding group members
        Collider2D[] nearby = Physics2D.OverlapCircleAll(selfPos, separationRadius);
        Vector2 separation = Vector2.zero;
        int neighborCount = 0;

        foreach (var collider in nearby)
        {
            if (collider.CompareTag("Spider") && collider.gameObject != gameObject)
            {
                Vector2 away = (selfPos - (Vector2)collider.transform.position).normalized;
                separation += away;
                neighborCount++;
            }
        }

        if (neighborCount > 0)
            flocking += (separation / neighborCount) * separationStrength;

        return flocking;
    }


    Vector2 ComputeSteering(Vector3 waypoint)
    {
        Vector2 selfPos = transform.position;
        Vector2 targetDir = ((Vector2)waypoint - selfPos).normalized;

        float[] weights = new float[RayCount];
        Vector2 resultant = Vector2.zero;

        for (int i = 0; i < RayCount; i++)
        {
            Vector2 dir = rayDirections[i];

            var hit = Physics2D.RaycastAll(selfPos, dir, rayDistance, obstacleMask);
            bool blocked = hit.Length > 1;
            if (blocked)
            {
                weights[i] = 0;
                continue;
            }

            float alignment = Mathf.Clamp01(Vector2.Dot(dir, targetDir));
            float patternBias = pattern[i % pattern.Length];

            float weight = alignment * patternBias;
            weights[i] = weight;

            resultant += dir * weight;
        }

        // Add obstacle avoidance
        Vector2 avoidance = ComputeObstacleAvoidance();
        float distToWaypoint = Vector2.Distance(selfPos, waypoint);
        
        // Reduce avoidance influence when very close to waypoint to prevent getting stuck
        if (distToWaypoint < 1f)
        {
            avoidance *= 0.3f;
            resultant = Vector2.Lerp(resultant, (Vector2)waypoint.normalized - selfPos, 0.6f);
        }
        
        resultant += avoidance;

        // Add flocking forces (cohesion + separation)
        Vector2 flocking = ComputeFlocking();
        resultant += flocking;

        // Smooth steering
        smoothedSteering = Vector2.Lerp(smoothedSteering, resultant, steeringSmooth);

        return smoothedSteering;
    }


    void ApplySteering(Vector2 steering)
    {
        if (steering.sqrMagnitude > 0.0001f)
        {
            Vector2 desiredVelocity = steering.normalized * speed;
            rb.linearVelocity = desiredVelocity;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, speed);
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
