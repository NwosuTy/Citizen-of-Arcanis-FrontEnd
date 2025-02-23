using TMPro;
using Ink.Runtime;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DialoguePanel : MonoBehaviour
{
    private Coroutine typingCoroutine;
    private WaitForSeconds typingSpeed;
    private static DialoguePanel Instance;

    [Header("Panel")]
    public Transform dialoguePanel;

    [Header("Speaker Panel")]
    public GameObject speakerObject;
    public TextMeshProUGUI speakerName;
    public TextMeshProUGUI speakerDialogue;

    [Header("Choices")]
    [SerializeField] private Transform choicesDrawer;
    [SerializeField] private List<DialoguePanelChoices> choicesUIList;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("A duplicate DialogueManager was found and destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        typingSpeed = new WaitForSeconds(0.05f);
    }

    public void StopDisplayCoroutine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
    }

    public void DisplayText(GameObject speaker, string dialogueText)
    {
        speakerName.text = speaker.name;
        speakerObject.gameObject.SetActive(false);
        speakerObject.gameObject.SetActive(true);
        typingCoroutine = StartCoroutine(StartTypingText(dialogueText));
    }

    public void DisableUIChoices()
    {
        foreach (var choicesUI in choicesUIList)
        {
            choicesUI.gameObject.SetActive(false);
        }
    }

    public void DisplayChoicesUI(Story story)
    {
        print(3);
        DisableUIChoices();
        if (story.currentChoices.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < story.currentChoices.Count; i++)
        {
            Choice choice = story.currentChoices[i];
            DialoguePanelChoices choiceUI = choicesUIList[i];

            choiceUI.Initialize(i, choice, this);
            choiceUI.gameObject.SetActive(true);
        }
        StartCoroutine(SelectFirstChoice());
    }

    public void ExitPanel()
    {
        speakerDialogue.text = "";
        gameObject.SetActive(false);
    }

    private IEnumerator SelectFirstChoice()
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(choicesUIList[0].gameObject);
    }

    private IEnumerator StartTypingText(string text)
    {
        DialogueManager.Instance.canContinue = false;

        speakerDialogue.text = "";
        yield return typingSpeed;

        speakerDialogue.text = text;
        DialogueManager.Instance.canContinue = true;
    }
}
