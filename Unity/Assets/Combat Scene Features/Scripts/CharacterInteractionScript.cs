using TMPro;
using UnityEngine;

public class CharacterInteractionScript : MonoBehaviour
{
    private Collider[] colliderArray;
    private CharacterManager characterManager;

    [Header("Interact UI")]
    [SerializeField] private GameObject interactUI;
    [SerializeField] private TextMeshProUGUI interactText;

    [field: Header("Combat Character")]
    [field: SerializeField] public CharacterCombatData CombatCharacter { get; private set; }

    private void Awake()
    {
        characterManager = GetComponent<CharacterManager>();
    }

    private void Start()
    {
        if (characterManager != null && characterManager.combatMode)
        {
            return;
        }
        colliderArray = new Collider[20];
        CombatManager combatManager = CombatManager.Instance;
        if(combatManager != null ) { combatManager.AssignPlayer(CombatCharacter.characterManager); }
    }

    private void Update()
    {
        DialogueManager instance = DialogueManager.Instance;
        if(instance != null && instance.dialogueIsPlaying)
        {
            interactUI.SetActive(false);
            return;
        }

        if(characterManager != null && characterManager.combatMode)
        {
            interactUI.SetActive(false);
            return;
        }

        IInteractable interactable = GetInteractableObject();
        if(interactable != null )
        {
            interactText.text = interactable.GetInteractText();
            interactUI.SetActive(true);
            if (Input.GetKeyDown(KeyCode.E))
            {
                interactable.Interact();
                interactUI.SetActive(false);
            }
        }
        else
        {
            interactUI.SetActive(false);
        }
    }

    private IInteractable GetInteractableObject()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, 3.0f, colliderArray);
        for (int i = 0; i < count; i++)
        {
            Transform interactObject = colliderArray[i].transform;
            IInteractable interactable = interactObject.GetComponentInParent<IInteractable>();

            if (interactable == null)
            {
                continue;
            }
            return interactable;
        }
        return null;
    }
}

public interface IInteractable
{
    public void Interact();
    public string GetInteractText();
}
