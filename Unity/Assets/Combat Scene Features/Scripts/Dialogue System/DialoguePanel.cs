using TMPro;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DialoguePanel : MonoBehaviour
{
    private bool skipFlag;
    private bool isTyping;

    private Coroutine typingCoroutine;
    private WaitForSeconds typingSpeed;
    private static DialoguePanel Instance;

    [Header("Panel")]
    public Transform dialoguePanel;
    [SerializeField] private Button skipButton;

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
        if (skipButton != null) skipButton.onClick.AddListener(HandleSkip);
    }

    public void HandleSkip()
    {
        if (isTyping == false)
        {
            return;
        }
        skipFlag = true;
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
        DialogueManager dialogueManager = DialogueManager.Instance;

        isTyping = true;
        dialogueManager.canContinue = false;

        speakerDialogue.text = "";
        foreach (char c in text)
        {
            if (skipFlag == true)
            {
                speakerDialogue.text = text;
                break;
            }
            speakerDialogue.text += c;
            yield return typingSpeed;
        }

        isTyping = false;
        skipFlag = false;
        dialogueManager.canContinue = true;
    }
}
