using TMPro;
using UnityEngine;
using Ink.Runtime;
using UnityEngine.UI;

public class DialoguePanelChoices : MonoBehaviour
{
    public Button choiceButton;
    private Choice dialoguechoice;
    public TextMeshProUGUI choiceText;

    public void Initialize(int choiceIndex, Choice choice, DialoguePanel uiPanel)
    {
        dialoguechoice = choice;
        choiceText.text = dialoguechoice.text;
        choiceButton.onClick.AddListener(() => MakeChoice(choiceIndex, uiPanel));
    }

    private void MakeChoice(int choiceIndex, DialoguePanel uiPanel)
    {
        DialogueManager.Instance.OnChoiceSelected(choiceIndex);

        uiPanel.speakerDialogue.text = dialoguechoice.text;
        uiPanel.DisableUIChoices();
    }
}
