using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "Pursue", menuName = "AIState/Patrol")]
public class PatrolState : AIState
{
    private float timeInState;
    private bool destinationSet;
    private Vector3 patrolDestination;

    [Header("Parameters")]
    [SerializeField] private float sphereRadius;
    [SerializeField] private float defaultTimer;
    [field: SerializeField] public PatrolMode patrolMode { get; private set; }

    public void Initialize()
    {
        timeInState = defaultTimer;
    }

    public override AIState StateUpdater(CharacterManager characterManager)
    {
        CharacterAnim animManager = characterManager.AnimatorManagaer;
        CharacterMovement moveManager = characterManager.MovementManager;

        if (characterManager.performingAction)
        {
            animManager.SetBlendTreeParameter(0f, 0f, false, Time.deltaTime);
            return this;
        }

        if(characterManager.Target != null)
        {
            return SwitchState(characterManager, characterManager.Pursue);
        }

        moveManager.RotateTowardsTarget();
        if (patrolMode == PatrolMode.Idle)
        {
            return IdleState(animManager, characterManager);
        }
        return WalkingState(animManager, moveManager, characterManager);
    }

    private AIState IdleState(CharacterAnim characterAnim, CharacterManager character)
    {
        float delta = Time.deltaTime;

        timeInState -= delta;
        character.Agent.enabled = false;
        characterAnim.SetBlendTreeParameter(0.0f, 0.0f, false, delta);

        if(timeInState <= 0.0f)
        {
            if(destinationSet != true)
            {
                SetDestination(character);
            }
            else
            {
                destinationSet = false;
                timeInState = defaultTimer + 2.0f;

                patrolMode = PatrolMode.Walk;
                character.Agent.enabled = true;
            }
        }
        return this;
    }

    private AIState WalkingState(CharacterAnim characterAnim, CharacterMovement movement, CharacterManager character)
    {
        if (character.dontMove == true)
        {
            return this;
        }

        timeInState -= Time.deltaTime;
        character.PatrolParametersSet(patrolDestination);

        NavMeshAgent agent = character.Agent;
        character.transform.rotation = character.Agent.transform.rotation;

        if (timeInState <= 0.0f || character.DistanceToTarget < agent.stoppingDistance)
        {
            agent.enabled = false;
            timeInState = defaultTimer;
            patrolMode = PatrolMode.Idle;
        }
        else
        {
            characterAnim.SetBlendTreeParameter(1.0f, 0.0f, false, Time.deltaTime);
            movement.MoveToDestination(0.25f, patrolDestination);
        }
        return this;
    }

    private void SetDestination(CharacterManager characterManager)
    {
        Vector3 offsetDirection = Random.insideUnitSphere.normalized * 7.0f;
        Vector3 offsetPosition = characterManager.transform.position + offsetDirection;
        Vector3 randomPoint = Random.insideUnitSphere * sphereRadius + offsetPosition;

        if(NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, sphereRadius, NavMesh.AllAreas))
        {
            destinationSet = true;
            patrolDestination = hit.position;
            characterManager.PatrolParametersSet(patrolDestination);
            return;
        }
        destinationSet = false;
    }
}