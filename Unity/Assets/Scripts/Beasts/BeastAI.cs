using UnityEngine;
using UnityEngine.AI;

public class BeastAI : MonoBehaviour
{
    public enum State { Idle, Chasing, Attacking }
    public State currentState = State.Idle;

    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;

    public float attackCooldown = 2f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!player)
            return;

        if (currentState == State.Chasing)
        {
            agent.SetDestination(player.position);
        }

        if (currentState == State.Attacking)
        {
            //TODO: ATTACK LOGIC
            transform.LookAt(player);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            currentState = State.Chasing;
            animator.SetBool("isMoving", true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            currentState = State.Idle;
            agent.ResetPath();
            animator.SetBool("isMoving", false);
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
