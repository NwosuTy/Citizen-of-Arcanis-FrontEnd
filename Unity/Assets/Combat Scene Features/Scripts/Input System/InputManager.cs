using DevionGames;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private InputControls control;
    private CharacterManager characterManager;
    public static InputManager Instance { get; private set; }

    private int clickCount = 0;
    private float lastClickTime = 0.0f;

    private bool attackInput;
    private Vector2 movementInput;
    [HideInInspector] public bool jumpInput;
    [SerializeField] private float doubleClickTime = 0.3f;
    
    public bool dashInput { get; private set; }
    public bool lockedInput { get; private set; }
    public bool interactInput { get; private set; }

    public bool holdControl { get; private set; }
    public bool lightAttackInput { get; private set; }
    public bool heavyAttackInput { get; private set; }

    public float moveAmount { get; private set; }
    public float verticalMoveInput { get; private set; }
    public float horizontalMoveInput { get; private set; }
    public Vector2 cameraInput { get; private set; }

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        characterManager = GetComponent<CharacterManager>();
    }

    private void OnEnable()
    {
        if(control == null)
        {
            control = new();

            control.BasicControl.CameraMovement.performed += ctx => cameraInput = ctx.ReadValue<Vector2>();
            control.BasicControl.CharacterMovement.performed += ctx => movementInput = ctx.ReadValue<Vector2>();

            control.AuxillaryControl.Control.performed += ctx => holdControl = true;
            control.AuxillaryControl.Control.canceled += ctx => holdControl = false;

            control.BasicControl.Interact.performed += ctx => interactInput = true;
            control.BasicControl.Jump.started += ctx => jumpInput = ctx.ReadValueAsButton();
            control.BasicControl.Jump.canceled += ctx => jumpInput = ctx.ReadValueAsButton();

            control.AuxillaryControl.Dash.performed += ctx => dashInput = true;
            control.AuxillaryControl.Dash.canceled += ctx => dashInput = false;
            control.AuxillaryControl.LockedIn.performed += ctx => lockedInput = true;
            control.AuxillaryControl.LockedIn.canceled += ctx => lockedInput = false;

            control.AuxillaryControl.AttackInput.performed += ctx => attackInput = true;
            control.AuxillaryControl.AttackInput.canceled += ctx => attackInput = false;
        }
        control.Enable();
    }

    private void OnDisable()
    {
        control.Disable();
    }

    public void InputManager_Update()
    {
        HandleMovement();
        HandleAttackInput();
    }

    public bool IsCurrentDeviceMouse()
    {
        //Check if there is an active mouse, and if it is in use
        if (Mouse.current != null && Mouse.current.enabled && Mouse.current.delta.ReadValue() != Vector2.zero)
        {
            return true;
        }
        return false;
    }


    private void HandleMovement()
    {
        verticalMoveInput = movementInput.y;
        horizontalMoveInput = movementInput.x;

        moveAmount = Mathf.Clamp01(Mathf.Abs(verticalMoveInput) + Mathf.Abs(horizontalMoveInput));
        characterManager.isMoving = (moveAmount > 0.0f);
    }

    public void ResetInput()
    {
        interactInput = false;
        lightAttackInput = false;
        heavyAttackInput = false;
    }

    private void HandleAttackInput()
    {
        if (attackInput)
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
