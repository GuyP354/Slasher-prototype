using UnityEngine;

public class AIMovement : MonoBehaviour
{
    private UnityEngine.AI.NavMeshAgent agent; 
    private void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }
    private void Update()
    {
        agent.SetDestination(GameObject.FindGameObjectWithTag("Target").transform.position);
    }
}
