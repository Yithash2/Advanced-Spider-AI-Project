using System.Collections;
using UnityEngine;
using UnityEngine.U2D.IK;

public class LegAnimator : MonoBehaviour
{
    [SerializeField] private int legId;
    
    public float SpeedOfMovement = 1 / 0.2f;

    [SerializeField] private spider_script spider;
    
    [SerializeField]
    private Transform bodyTarget;
    [SerializeField]
    private Transform movableTarget; // Used for sticking the legs to walls
    
    [SerializeField, Range(0,2f)]
    private float distanceThreshold, maxDistance, maxDetachingDistance;
    
    //public float DistanceByMovement {get {return distanceThreshold*1.5f;}}
    
    public bool IsAnimating { get; private set; }
    public bool ReachedMidPoint { get; private set; }
    
    [SerializeField]
    LayerMask layerMask;

    public bool Ignore = false;
    private bool blocked = false;

    public int currentSolversTarget = 0;
    public Transform[] targets;

    [SerializeField] private LimbSolver2D Solver;

    public void Update()
    {
        if (targets[currentSolversTarget] != Solver.GetChain(0).target)
        {
            Solver.GetChain(0).target = targets[currentSolversTarget];
        }

        UpdateMovableTarget();
    }

    private void UpdateMovableTarget()
    {
        var hits = Physics2D.CircleCast(bodyTarget.position, 0.4f,Vector2.right,0.4f,layerMask.value);

        if (hits && !spider.IsMyOwnLeg(hits.collider))
        {
            blocked = true;
            movableTarget.position = hits.point;
            if (hits.collider.gameObject.layer == LayerMask.NameToLayer("Spider") )
            {
                //spider.Blush(true,legId);
            }
        }
        else if(!blocked|| Vector2.Distance(movableTarget.position, bodyTarget.position) > maxDetachingDistance)
        {
            //Debug.Log("UnBlushing");
            blocked = false;
            movableTarget.position = bodyTarget.position;
            spider.Blush(false,legId);
        }
    }
    
    public void Animate(float Speed)
    {
        if (Ignore) return;

        /*if (blocked)
        {
            if (!IsAnimating) {
                IsAnimating = true;
                StartCoroutine(MoveLegStuckToObject());
            }
        }
        else
        {*/
            if(Vector3.Distance(movableTarget.position, transform.position) <= distanceThreshold) return;
            
            if (Vector3.Distance(movableTarget.position, transform.position) > maxDistance)
            {
                if (!IsAnimating) {
                    IsAnimating = true;

                    StartCoroutine(MoveLegToRest());
                }  
            }
            else
            {
                if (!IsAnimating) {
                    IsAnimating = true;
                    StartCoroutine(MoveLegToMove(Speed));
                }
            }
                    
           
        //}
        
        
    }

    private float AnimationCurve()
    {
        return -Mathf.Exp(-SpeedOfMovement / 5);
    }

    private IEnumerator MoveLegToMove(float speed)
    {
        
        var dirTotarget = (movableTarget.position - transform.position).normalized;
        var nextPosition = movableTarget.position + (dirTotarget * (speed * Time.deltaTime));
        var midPoint = (nextPosition + transform.position) * 0.5f;
        midPoint.y += 0.5f;
        
        while (Vector3.Distance(midPoint, transform.position) > 0.01f)
        {
            transform.position = midPoint;
            if (Ignore)
            {
                ReachedMidPoint = false;
                IsAnimating = false;
                yield break;
            }
            yield return 0;
        }
        ReachedMidPoint = true;
        nextPosition = movableTarget.position + (dirTotarget * (speed * Time.deltaTime));
        while (Vector3.Distance(nextPosition, transform.position) > 0.01f)
        {
            transform.position = nextPosition;

            if (Ignore)
            {
                ReachedMidPoint = false;
                IsAnimating = false;
                yield break;
            }
            yield return 0;
        }
        ReachedMidPoint = false;
        IsAnimating = false;
        
    }
    private IEnumerator MoveLegToRest()
    {
        
        var nextPosition = movableTarget.position;
        var midPoint = (nextPosition + transform.position) * 0.5f;
        midPoint.y += 0.5f;
        
        while (Vector3.Distance(midPoint, transform.position) > 0.01f)
        {
            transform.position = midPoint;
            if (Ignore)
            {
                ReachedMidPoint = false;
                IsAnimating = false;
                yield break;
            }
            yield return 0;
        }
        ReachedMidPoint = true;
        nextPosition = movableTarget.position;
        while (Vector3.Distance(nextPosition, transform.position) > 0.01f)
        {
            transform.position = nextPosition;

            if (Ignore)
            {
                ReachedMidPoint = false;
                IsAnimating = false;
                yield break;
            }
            yield return 0;
        }
        ReachedMidPoint = false;
        IsAnimating = false;
        
    }
    
    private IEnumerator MoveLegStuckToObject()
    {
        var nextPosition = movableTarget.position;
        
        while (Vector3.Distance(nextPosition, transform.position) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(transform.position, nextPosition, SpeedOfMovement * Time.deltaTime);
            if (Ignore)
            {
                ReachedMidPoint = false;
                IsAnimating = false;
                yield break;
            }
            yield return 0;
        }
        ReachedMidPoint = false;
        IsAnimating = false;
        
    }
}
