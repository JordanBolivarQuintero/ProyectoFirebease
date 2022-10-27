using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RequestController : MonoBehaviour
{
    public void RemoveRequest()
    {
        StartCoroutine(DeleteRequest(gameObject.name));
    }
    IEnumerator DeleteRequest(string aID)
    {
        var DBTask = AuthManager.DBreference.Child("users").Child(AuthManager.User.UserId).Child("requests").Child(aID).RemoveValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            Debug.Log("request from " + aID + " delete");
        }
    }
    public void ConfirmRequest(TMP_Text name)
    {
        StartCoroutine(addFriend(gameObject.name, name.text));
    }
    IEnumerator addFriend(string aID, string name)
    {        
        var DBTask1 = AuthManager.DBreference.Child("users").Child(AuthManager.User.UserId).Child("friends").Child(aID).SetValueAsync(name.ToString());

        yield return new WaitUntil(predicate: () => DBTask1.IsCompleted);

        if (DBTask1.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask1.Exception}");
        }
        else
        {
            var DBTask2 = AuthManager.DBreference.Child("users").Child(aID).Child("friends").Child(AuthManager.User.UserId).SetValueAsync(AuthManager.User.DisplayName.ToString());
            yield return new WaitUntil(predicate: () => DBTask2.IsCompleted);

            if (DBTask2.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {DBTask2.Exception}");
            }
            else
            {
                Debug.Log("now you're friend of " + name);
                StartCoroutine(DeleteRequest(gameObject.name));
            }
        }
    }
}
