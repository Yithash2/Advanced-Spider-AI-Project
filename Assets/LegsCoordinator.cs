using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class LegsCoordinator : MonoBehaviour
{
    [SerializeField]
    private LegAnimator[] legs;
    private int curentIndex;
    
    [SerializeField]
    private float timeBetweenLegs;

    private float counter;
    

    public void UpdateAnimation(float speed)
    {
        if (counter < timeBetweenLegs)
        {
            counter += Time.deltaTime;
        }
        else
        {
            counter = 0;
            legs[curentIndex].Animate(speed);
            curentIndex = (curentIndex + 1) % legs.Length;
        }
        
    }

    public void CalculateLegsSpeed(float speed)
    {
        foreach (LegAnimator leg in legs)
        {
            leg.SpeedOfMovement = speed * 2;
        }
        timeBetweenLegs = speed*Time.deltaTime/8;
        
    }

}
