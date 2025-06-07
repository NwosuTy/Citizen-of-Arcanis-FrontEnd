using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "Pursue", menuName = "AIState/Pursue")]
public class PursueState : AIState
{
    public override AIState StateUpdater(CharacterManager characterManager)
    {
        NavMeshAgent agent = characterManager.Agent;
        CharacterAnim animManager = characterManager.AnimatorManagaer;
        CharacterMovement moveManager = characterManager.MovementManager;

        if(characterManager.performingAction || characterManager.Target == null)
        {
            animManager.SetBlendTreeParameter(0f, 0f, false, Time.deltaTime);
            return this;
        }

        if(characterManager.dontMove != true && agent.enabled != true)
        {
            agent.enabled = true;
        }
        moveManager.RotateTowardsTarget();

        bool notClose = characterManager.DistanceToTarget >= agent.stoppingDistance;
        if(notClose)
        {
            animManager.SetBlendTreeParameter(1.0f, 0.0f, false, Time.deltaTime);
            moveManager.MoveToDestination(1.0f, characterManager.PositionOfTarget);
            return this;
        }

        if(characterManager.combatMode != true)
        {
            if (characterManager.hasReached != true)
            {
                characterManager.Assignment?.Invoke();
            }
            return this;
        }
        characterManager.isMoving = false;
        return SwitchState(characterManager, characterManager.Combat);
    }
}