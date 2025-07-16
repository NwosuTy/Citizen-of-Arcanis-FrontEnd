using UnityEngine;

public class CharacterAnim : MonoBehaviour
{
    private bool hasHashed;

    private int rotateHash;
    private int movingHash;
    private int talkingHash;
    private int groundedHash;
    private int performActionHash;

    private int verticalMovementHash;
    private int horizontalMovementHash;
    private CharacterManager characterManager;

    [SerializeField] private AnimatorOverrideController unarmedController;

    void Awake()
    {
        characterManager = GetComponent<CharacterManager>();
    }

    private void Start()
    {
        characterManager.Anim.runtimeAnimatorController = unarmedController;
    }

    private void OnEnable()
    {
        if(hasHashed == true)
        {
            return;
        }

        movingHash = Animator.StringToHash("isMoving");
        rotateHash = Animator.StringToHash("canRotate");
        talkingHash = Animator.StringToHash("isTalking");
        groundedHash = Animator.StringToHash("isGrounded");
        performActionHash = Animator.StringToHash("performAction");

        verticalMovementHash = Animator.StringToHash("verticalMovement");
        horizontalMovementHash = Animator.StringToHash("horizontalMovement");
    }

    private void OnDisable()
    {
        if(hasHashed == true)
        {
            hasHashed = false;
        }
    }

    private void OnAnimatorMove()
    {
        if (characterManager.isGrounded != true)
        {
            return;
        }
        Animator animator = characterManager.Anim;

        Vector3 deltaPosition = animator.deltaPosition;
        characterManager.Controller.Move(deltaPosition);
        characterManager.transform.rotation *= animator.deltaRotation;
    }

    public void PlayTargetAnimation(int targetAnimation, bool performingAction, float transitionDuration = 0.2f, bool canRotate = true)
    {
        Animator animator = characterManager.Anim;

        animator.applyRootMotion = performingAction;
        characterManager.canRotate = canRotate;
        
        animator.SetBool(performActionHash, performingAction);
        animator.CrossFade(targetAnimation, transitionDuration);
    }

    public void PlayAttackAnimation(int targetAnimation, bool performingAction, bool shouldMirror, float transitionDuration = 0.2f, bool canRotate = true)
    {
        Animator animator = characterManager.Anim;
        animator.applyRootMotion = performingAction;

        characterManager.canRotate = canRotate;
        characterManager.CombatManager.SetMirrorStatus(shouldMirror, animator);

        animator.SetBool(performActionHash, performingAction);
        animator.CrossFade(targetAnimation, transitionDuration);
    }

    public void CanRotate()
    {
        characterManager.Anim.SetBool(rotateHash, true);
    }

    public void CantRotate()
    {
        characterManager.Anim.SetBool(rotateHash, false);
    }

    public void SetBlendTreeParameter(float verticalInput, float horizontalInput, bool isSprinting, float delta)
    {
        float snappedVertical = verticalInput;
        float snappedHorizontal = horizontalInput;
        Animator animator = characterManager.Anim;

        if (isSprinting)
        {
            snappedVertical = 2.0f;
            snappedHorizontal = 0.0f;
        }
        animator.SetFloat(verticalMovementHash, snappedVertical, 0.1f, delta);
        animator.SetFloat(horizontalMovementHash, snappedHorizontal, 0.1f, delta);
    }

    public void SetAnimatorBool(Animator animator)
    {
        animator.SetBool(movingHash, characterManager.isMoving);
        animator.SetBool(talkingHash, characterManager.isTalking);
        animator.SetBool(groundedHash, characterManager.isGrounded);

        characterManager.canRotate = animator.GetBool(rotateHash);
        characterManager.performingAction = animator.GetBool(performActionHash);
    }

    public void SetAnimator(AnimatorOverrideController controller)
    {
        Animator anim = characterManager.Anim;
        anim.runtimeAnimatorController = (controller == null) ? unarmedController : controller;
    }
}