using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendRequestButton : MonoBehaviour
{
    public void SendRequest()
    {
        StartCoroutine(UploadRequest(gameObject.name));
    }
    IEnumerator UploadRequest(string aID)
    {
        var DBTask = AuthManager.DBreference.Child("users").Child(aID).Child("requests").Child(AuthManager.User.UserId).SetValueAsync(AuthManager.User.DisplayName.ToString());

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            Debug.Log("request send to " + aID);
        }
    }
}
