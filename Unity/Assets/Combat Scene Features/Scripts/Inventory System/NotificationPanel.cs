using TMPro;
using UnityEngine;
using System.Collections;

public class NotificationPanel : MonoBehaviour
{
    private WaitForSeconds waitForSeconds;

    [Header("Panel UI")]
    [SerializeField] private float disableDelay;
    [SerializeField] private TextMeshProUGUI notificationText;

    private void Awake()
    {
        waitForSeconds = new WaitForSeconds(disableDelay);
    }

    public void ShowNotification(string notification)
    {
        gameObject.SetActive(true);
        StartCoroutine(Notification(notification));
    }

    private IEnumerator Notification(string text)
    {
        notificationText.text = text;
        yield return waitForSeconds;

        notificationText.text = "";
        gameObject.SetActive(false);
    }
}
