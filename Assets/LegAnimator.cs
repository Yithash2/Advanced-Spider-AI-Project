using System.Collections;
using UnityEngine;
using UnityEngine.U2D.IK;

public class LegAnimator : MonoBehaviour
{
    [SerializeField]
    private Transform bodyTarget;
    
    [SerializeField, Range(0,2f)]
    private float distanceThreshold;
    
    public bool IsAnimating { get; private set; }
    public bool ReachedMidPoint { get; private set; }

    public bool Ignore = false;

    public int currentSolversTarget = 0;
    public Transform[] targets;

    [SerializeField] private LimbSolver2D Solver;

    public void Update()
    {
        if (targets[currentSolversTarget] != Solver.GetChain(0).target)
        {
            Solver.GetChain(0).target = targets[currentSolversTarget];
        }
    }
    
    public void Animate()
    {
        if (Ignore) return;
        
        if(Vector3.Distance(bodyTarget.position, transform.position) <= distanceThreshold) return;
        
        if (!IsAnimating) {
            IsAnimating = true;
            StartCoroutine(MoveLeg());
        }
    }

    private IEnumerator MoveLeg()
    {
        
        var dirTotarget = (bodyTarget.position - transform.position).normalized;
        var nextPosition = bodyTarget.position + (dirTotarget * (distanceThreshold * 1.1f));
        var midPoint = (nextPosition + transform.position) * 0.5f;
        midPoint.y += 0.5f;
        
        while (Vector3.Distance(midPoint, transform.position) > 0.01f)
        {
            transform.position = Vector2.Lerp(transform.position, midPoint, 0.125f);
            if (Ignore)
            {
                ReachedMidPoint = false;
                IsAnimating = false;
                yield break;
            }
            yield return 0;
        }
        ReachedMidPoint = true;
        nextPosition = bodyTarget.position + (dirTotarget * (distanceThreshold * 1.1f));
        while (Vector3.Distance(nextPosition, transform.position) > 0.01f)
        {
            transform.position = Vector2.Lerp(transform.position, nextPosition, 0.125f);
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
