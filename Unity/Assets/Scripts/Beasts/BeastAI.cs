using UnityEngine;
using UnityEngine.AI;

public class BeastAI : MonoBehaviour
{
    public enum State { Idle, Chasing, Attacking }
    public State currentState = State.Idle;

    private Transform player;
    private NavMeshAgent agent;

    public float attackCooldown = 2f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (currentState == State.Chasing)
        {
            agent.SetDestination(player.position);
        }

        if (currentState == State.Attacking)
        {
            transform.LookAt(player);

            Debug.LogWarning("Attack");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.LogWarning(other.tag);
        if (other.CompareTag("Player"))
        {
            currentState = State.Chasing;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            currentState = State.Idle;
            agent.ResetPath();
        }
    }

    public void OnAttackRangeEnter()
    {
        if (currentState == State.Chasing)
        {
            currentState = State.Attacking;
            agent.ResetPath();
        }
    }

    public void OnAttackRangeExit()
    {
        if (currentState == State.Attacking)
        {
            currentState = State.Chasing;
        }
    }
}
