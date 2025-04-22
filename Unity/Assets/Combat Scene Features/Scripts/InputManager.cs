using UnityEngine;

public class InputManager : MonoBehaviour
{
    private CharacterManager characterManager;

    private int clickCount = 0;
    private float lastClickTime = 0.0f;
    [SerializeField] private float doubleClickTime = 0.3f;

    public bool jumpInput { get; private set; }
    public bool dashInput { get; private set; }
    public bool lockedInput { get; private set; }
    public bool lightAttackInput { get; private set; }
    public bool heavyAttackInput { get; private set; }

    public float moveAmount { get; private set; }
    public float verticalMoveInput { get; private set; }
    public float horizontalMoveInput { get; private set; }

    private void Awake()
    {
        characterManager = GetComponent<CharacterManager>();
    }

    public void InputManager_Update()
    {
        HandleMovement();

        HandleAttackInput();
        lockedInput = Input.GetMouseButtonDown(1);

        dashInput = Input.GetKey(KeyCode.LeftShift);
        jumpInput = Input.GetKeyDown(KeyCode.Space);
    }

    private void HandleMovement()
    {
        verticalMoveInput = Input.GetAxis("Vertical");
        horizontalMoveInput = Input.GetAxis("Horizontal");

        moveAmount = Mathf.Clamp01(Mathf.Abs(verticalMoveInput) + Mathf.Abs(horizontalMoveInput));
        characterManager.isMoving = (moveAmount > 0.0f);
    }

    public void ResetInput()
    {
        lightAttackInput = false;
        heavyAttackInput = false;
    }

    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            clickCount++;
            if (clickCount == 1)
            {
                lastClickTime = Time.time;
                Invoke(nameof(SingleClickAction), doubleClickTime);
            }
            else if (clickCount == 2 && (Time.time - lastClickTime) < doubleClickTime)
            {
                CancelInvoke(nameof(SingleClickAction));
                heavyAttackInput = true;
                clickCount = 0;
            }
        }

        if(Time.time - lastClickTime > doubleClickTime)
        {
            clickCount = 0;
        }
    }

    private void SingleClickAction()
    {
        if(clickCount == 1)
        {
            lightAttackInput = true;
        }
        clickCount = 0;
    }
}
