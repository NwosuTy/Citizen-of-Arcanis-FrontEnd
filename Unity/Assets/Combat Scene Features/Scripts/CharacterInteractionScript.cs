using TMPro;
using UnityEngine;

public class CharacterInteractionScript : MonoBehaviour
{
    private Collider[] colliderArray;
    private CharacterManager characterManager;

    [Header("Interact UI")]
    [SerializeField] private GameObject interactUI;
    [SerializeField] private TextMeshProUGUI interactText;
    [Range(0f,5.0f)] [SerializeField] private float detectRadius = 1.0f;

    private void Awake()
    {
        characterManager = GetComponent<CharacterManager>();
    }

    private void Start()
    {
        colliderArray = new Collider[20];
    }

    private void Update()
    {
        DialogueManager instance = DialogueManager.Instance;
        if(instance != null && instance.dialogueIsPlaying)
        {
            interactUI.SetActive(false);
            return;
        }

        IInteractable interactable = GetInteractableObject();
        if(interactable != null )
        {
            bool inCombat = (characterManager != null && characterManager.combatMode);
            if (interactable is DialogueTrigger && inCombat)
            {
                return;
            }

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
        int count = Physics.OverlapSphereNonAlloc(transform.position, detectRadius, colliderArray);
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }

}
