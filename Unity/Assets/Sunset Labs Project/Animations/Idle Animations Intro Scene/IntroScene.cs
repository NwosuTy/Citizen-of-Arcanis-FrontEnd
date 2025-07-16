using UnityEngine;

public class IntroScene : MonoBehaviour
{
    public void LoadSelectionScene()
    {
        PlayerPrefs.SetInt("HasPlayedIntro", 1);
        StartCoroutine(LevelLoader.LoadSceneAsync("Character Selection Scene"));
    }
}
