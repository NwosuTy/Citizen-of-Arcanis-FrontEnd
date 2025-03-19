using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private float remainingTime;
    [HideInInspector] public bool timeUp;

    [Header("Timer")]
    [SerializeField] private int maxTime;
    [SerializeField] private TextMeshProUGUI countdownTimer;

    [Header("Player Info")]
    [SerializeField] private PlayerUI enemyUI;
    [SerializeField] private PlayerUI playerUI;

    public void PrepareTimer()
    {
        remainingTime = maxTime;
        countdownTimer.color = Color.green;
        countdownTimer.text = remainingTime.ToString();
    }

    public void HandleCountdown(float delta)
    {
        remainingTime -= delta; 
        int remainingTimeInt = Mathf.FloorToInt(remainingTime);

        countdownTimer.text = remainingTimeInt.ToString();
        countdownTimer.color = (remainingTimeInt < maxTime/4) ? Color.red : Color.green;

        timeUp = (remainingTimeInt <= 0);
    }

    public void PrepareDuelingCharacter(CharacterManager characterManager)
    {
        if(characterManager.characterType == CharacterType.AI)
        {
            enemyUI.SetParameters(characterManager);
            return;
        }
        playerUI.SetParameters(characterManager);
    }
}
