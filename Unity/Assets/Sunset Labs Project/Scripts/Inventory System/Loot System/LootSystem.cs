using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LootSystem
{
    private static List<int> unExcludedIndex = new();

    public static ItemBox GetRandomBox(int maxRate, ItemBox exclude, ItemBox[] itemBoxes)
    {
        int random = 0;
        unExcludedIndex.Clear();

        for (int i = 0; i < itemBoxes.Length; i++)
        {
            ItemBox box = itemBoxes[i];
            if (box == null || box == exclude)
            {
                continue;
            }

            random = Random.Range(0, maxRate);
            if (box.MaxRate > random)
            {
                continue;
            }
            unExcludedIndex.Add(i);
        }
        random = Random.Range(0, unExcludedIndex.Count);
        ItemBox selectedItem = itemBoxes[random];
        return selectedItem;
    }

    public static PickableObject GetRandomItem(PickableObject exclude, PickableObject[] itemsArray, ItemBox itemBox)
    {
        int random = 0;
        unExcludedIndex.Clear();

        for (int i = 0; i < itemsArray.Length; i++)
        {
            PickableObject item = itemsArray[i];
            if (item == null || item == exclude)
            {
                continue;
            }

            random = Random.Range(0, itemBox.MaxRate);
            if (item.rewardRate > random)
            {
                continue;
            }
            unExcludedIndex.Add(i);
        }
        random = Random.Range(0, unExcludedIndex.Count);
        PickableObject wonItem = itemsArray[random];
        return wonItem;
    }
}

public static class LevelLoader
{
    private static AsyncOperation sceneLoadingOperation;
    public static void HandleLoadLevel(string levelName, System.Action action, MonoBehaviour monoBehaviour)
    {
        monoBehaviour.StartCoroutine(LoadLevelCoroutine(levelName, action));
    }

    public static IEnumerator LoadSceneAsync(string sceneName)
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        sceneLoadingOperation = SceneManager.LoadSceneAsync(sceneName);
        sceneLoadingOperation.allowSceneActivation = false;

        while(sceneLoadingOperation.isDone != true)
        {
            if(sceneLoadingOperation.progress >= 0.9f)
            {
                sceneLoadingOperation.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    public static IEnumerator LoadLevelCoroutine(string levelName, System.Action action)
    {
        sceneLoadingOperation = SceneManager.LoadSceneAsync(levelName);
        sceneLoadingOperation.completed += operation => { action?.Invoke(); };

        while(sceneLoadingOperation.isDone != true)
        {
            System.GC.Collect();
            yield return null;
        }
    }
}
