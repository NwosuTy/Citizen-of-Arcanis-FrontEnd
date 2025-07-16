using UnityEngine;

namespace Chidera_Nwosu
{
    public class RandomIdleAnimation : StateMachineBehaviour
    {
        private bool isBored;
        private float idleTime;
        private int randomHash;
        private int boredAnimation;

        private CharacterManager characterManager;

        [Header("Details")]
        [SerializeField] private bool combatCharacter;
        [SerializeField] private float timeUntilBored;
        [SerializeField] private int numberOfBoredAnimations;


        //OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if(combatCharacter && characterManager == null)
            {
                characterManager = animator.GetComponent<CharacterManager>();
            }

            ResetBoredom();
            if(randomHash == 0 )
            {
                randomHash = Animator.StringToHash("randomAnimation");
            }
        }

        //OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            float delta = Time.deltaTime;
            bool dontPlayRandom = DontPlayAnimation();

            if (isBored == false)
            {
                idleTime += delta;
                if (idleTime >= timeUntilBored)
                {
                    isBored = true;
                    boredAnimation = Random.Range(1, numberOfBoredAnimations + 1);
                    boredAnimation = boredAnimation * 2 - 1;
                    animator.SetFloat(randomHash, boredAnimation - 1);
                }
            }
            else if (stateInfo.normalizedTime % 1 >= 0.98f)
            {
                ResetBoredom();
            }
            int index = (dontPlayRandom) ? boredAnimation - 1 : boredAnimation;
            animator.SetFloat(randomHash, index, 0.2f, delta);
        }

        private bool DontPlayAnimation()
        {
            if(characterManager == null)
            {
                return false;
            }
            return (characterManager.isMoving || characterManager.performingAction);
        }

        private void ResetBoredom()
        {
            if (isBored)
            {
                boredAnimation--;
            }
            isBored = false;
            idleTime = 0.0f;
        }
    }
}
