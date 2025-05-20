using System;
using System.Text;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public static class StarkAPILink
{
    public static string ApiBaseURL = "http://localhost:3000";

    // Get user info or inventory
    public static IEnumerator Get(string userId, bool isUser, Action<string> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            onError?.Invoke("Missing or empty userId");
            yield break;
        }

        // Corrected endpoints: /users/:id or /inventory/:userId
        string route = isUser ? $"users/{userId}" : $"inventory/{userId}";
        string url = $"{ApiBaseURL}/{route}";

        using UnityWebRequest req = UnityWebRequest.Get(url);
        {
            Debug.Log($"GET: {url}");
            yield return req.SendWebRequest();
            HandleAction(req, onSuccess, onError);
        }
    }

    // Mint item for user
    public static IEnumerator MintItem(string u_Id, ItemType itemType, Action<string> onSuccess, Action<string> onError)
    {
        string url = $"{ApiBaseURL}/starknet/batch/mint";

        MintRequest body = new()
        {
            userId = u_Id,
            tokenIds = new[] { (int)itemType },
            amounts = new[] { 1 }
        };
        yield return PostJson(url, body, onSuccess, onError);
    }


    // Use item for user
    public static IEnumerator UseItem(string userId, string itemId, Action<string> onSuccess, Action<string> onError)
    {
        string url = $"{ApiBaseURL}/use-item";
        var body = new
        {
            userId = userId,
            item_id = itemId
        };
        yield return PostJson(url, body, onSuccess, onError);
    }

    private static bool IsSuccess(UnityWebRequest req)
    {
        return req.result == UnityWebRequest.Result.Success;
    }

    private static string FormatError(UnityWebRequest req)
    {
        string errorText = req.downloadHandler != null ? req.downloadHandler.text : "No response body";
        return $"Error({req.responseCode}) : {req.error} \n {errorText}";
    }

    private static void HandleAction(UnityWebRequest req, Action<string> onSuccess, Action<string> onError)
    {
        if (IsSuccess(req))
        {
            onSuccess?.Invoke(req.downloadHandler.text);
        }
        else
        {
            onError?.Invoke(FormatError(req));
        }
    }

    private static IEnumerator PostJson(string url, object body, Action<string> onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest req = new(url, "POST");
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"POST: {url} | Body: {json}");

            yield return req.SendWebRequest();
            HandleAction(req, onSuccess, onError);
        }
    }
}
