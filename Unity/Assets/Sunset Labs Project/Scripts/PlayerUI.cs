using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Player Information")]
    [SerializeField] private Image characterSprite;
    [SerializeField] private TextMeshProUGUI characterNameText;

    [Header("UI Bars")]
    [SerializeField] private UIBar healthBarUI;
    [SerializeField] private UIBar enduranceBarUI;

    public void SetParameters(CharacterManager character)
    {
        string displayName = character.name.Replace("(Clone)", "").Trim();

        characterNameText.text = displayName;
        characterSprite.sprite = character.CharacterImage;
        character.StatsManager.SetBarUIs(healthBarUI, enduranceBarUI);
    }
}
