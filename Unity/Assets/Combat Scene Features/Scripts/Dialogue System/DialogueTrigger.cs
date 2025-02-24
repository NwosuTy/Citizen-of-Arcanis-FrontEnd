using UnityEngine;
using System.Collections.Generic;

public class DialogueTrigger : MonoBehaviour, IInteractable
{
    private string interactText;
    private TextAsset currentDialogue;
    private CharacterManager character;

    [Header("Properties")]
    [SerializeField] private TextAsset[] dialogueTextsArray;

    private void Awake()
    {
        character = GetComponent<CharacterManager>();
        interactText = $"Press E To Interact With {this.gameObject.name}";
    }

    public void Interact()
    {
        character.dontMove = true;
        character.isTalking = true;
        currentDialogue = RandomText(currentDialogue);
        DialogueManager.Instance.HandleDialogue(currentDialogue, character);
    }

    private TextAsset RandomText(TextAsset exclude)
    {
        List<int> textIndex = new List<int>();
        for(int i = 0; i < dialogueTextsArray.Length; i++)
        {
            if (dialogueTextsArray[i] == exclude)
            {
                continue;
            }

            textIndex.Add(i);
        }
        int random = Random.Range(0, textIndex.Count);

        int index = textIndex[random];
        return dialogueTextsArray[index];
    }

    public string GetInteractText()
    {
        return interactText;
    }
}
