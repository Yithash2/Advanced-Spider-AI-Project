using UnityEngine;

public class SimpleCamFollow : MonoBehaviour
{
    [SerializeField]
    private Transform target;
    
    [SerializeField]
    private Vector3 offset;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, target.position + offset, 0.15f);
    }
}
