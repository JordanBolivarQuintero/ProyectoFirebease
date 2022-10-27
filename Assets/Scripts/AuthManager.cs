using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using TMPro;
using UnityEngine.UI;
using Firebase.Database;
using UnityEngine.SceneManagement;
using System.Linq;

public class AuthManager : MonoBehaviour
{
    //Firebase variables
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public static FirebaseAuth auth;
    public static FirebaseUser User;
    public static DatabaseReference DBreference;

    //Login variables
    [Header("Login")]
    public TMP_InputField emailLoginField;
    public TMP_InputField passwordLoginField;
    public TMP_Text warningLoginText;
    public TMP_Text confirmLoginText;

    //Register variables
    [Header("Register")]
    public TMP_InputField usernameRegisterField;
    public TMP_InputField emailRegisterField;
    public TMP_InputField passwordRegisterField;
    public TMP_InputField passwordRegisterVerifyField;
    public TMP_Text warningRegisterText;

    //Register variables
    [Header("Social")]
    public TMP_Text info;
    public Transform onlineParent;
    public Transform friendsParent;
    public Transform requestParent;
    //
    public GameObject online;
    public GameObject friend;
    public GameObject request;
    //
    public TMP_InputField RoomField;
    public Transform roomUserParent;
    public TMP_Text numberText;

    [Header("UserData")]
    [SerializeField] Transform texts;
    int number = 0;

    void Awake()
    {
        //Check that all of the necessary dependencies for Firebase are present on the system
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                //If they are avalible Initialize Firebase
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }
    private void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            FirebaseDatabase.DefaultInstance.GetReference("onlineUsers").ValueChanged += LoadOnlineUser;
            FirebaseDatabase.DefaultInstance.GetReference("users/" + User.UserId + "/friends").ValueChanged += LoadFriends;
            FirebaseDatabase.DefaultInstance.GetReference("users/" + User.UserId + "/requests").ValueChanged += LoadRequest;
            FirebaseDatabase.DefaultInstance.GetReference("room" + number).ValueChanged += LoadRoom;
        }
    }
    private void OnDestroy()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            FirebaseDatabase.DefaultInstance.GetReference("onlineUsers").ValueChanged -= LoadOnlineUser;
            FirebaseDatabase.DefaultInstance.GetReference("users/" + User.UserId + "/friends").ValueChanged -= LoadFriends;
            FirebaseDatabase.DefaultInstance.GetReference("users/" + User.UserId + "/requests").ValueChanged -= LoadRequest;
            FirebaseDatabase.DefaultInstance.GetReference("room" + number).ValueChanged -= LoadRoom;
        }
    }
    private void InitializeFirebase()
    {
        Debug.Log("Setting up Firebase Auth");
        //Set the authentication instance object
        auth = FirebaseAuth.DefaultInstance;
        DBreference = FirebaseDatabase.DefaultInstance.RootReference;
        User = auth.CurrentUser;
    }
    public void ClearLoginFeilds()
    {
        emailLoginField.text = "";
        passwordLoginField.text = "";
    }
    public void ClearRegisterFeilds()
    {
        usernameRegisterField.text = "";
        emailRegisterField.text = "";
        passwordRegisterField.text = "";
        passwordRegisterVerifyField.text = "";
    }

    //Function for the login button
    public void LoginButton()
    {
        //Call the login coroutine passing the email and password
        StartCoroutine(Login(emailLoginField.text, passwordLoginField.text));
    }
    //Function for the register button
    public void RegisterButton()
    {
        //Call the register coroutine passing the email, password, and username
        StartCoroutine(Register(emailRegisterField.text, passwordRegisterField.text, usernameRegisterField.text));
    }
    public void SignOutButton()
    {
        UpdateStatus(false); //new
        auth.SignOut();
        SceneManager.LoadScene(0);
        /*ClearRegisterFeilds();
        ClearLoginFeilds();*/
    }

    private IEnumerator Login(string _email, string _password)
    {
        //Call the Firebase auth signin function passing the email and password
        var LoginTask = auth.SignInWithEmailAndPasswordAsync(_email, _password);
        //Wait until the task completes
        yield return new WaitUntil(predicate: () => LoginTask.IsCompleted);

        if (LoginTask.Exception != null)
        {
            //If there are errors handle them
            Debug.LogWarning(message: $"Failed to register task with {LoginTask.Exception}");
            FirebaseException firebaseEx = LoginTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Login Failed!";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.WrongPassword:
                    message = "Wrong Password";
                    break;
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
                case AuthError.UserNotFound:
                    message = "Account does not exist";
                    break;
            }
            warningLoginText.text = message;
        }
        else
        {
            //User is now logged in
            //Now get the result

            User = LoginTask.Result;

            if (User.IsEmailVerified)
            {
                Debug.LogFormat("User signed in successfully: {0} ({1})", User.DisplayName, User.Email);
                warningLoginText.text = "";
                confirmLoginText.text = "Logged In";
                UpdateStatus(true); //new
                StartCoroutine(LoadGameScene());
            }
            else
            {
                StartCoroutine(SendVerificationEmail());
            }

        }
    }

    private IEnumerator LoadGameScene()
    {
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(1);
    }

    private IEnumerator Register(string _email, string _password, string _username)
    {
        if (_username == "")
        {
            //If the username field is blank show a warning
            warningRegisterText.text = "Missing Username";
        }
        else if (passwordRegisterField.text != passwordRegisterVerifyField.text)
        {
            //If the password does not match show a warning
            warningRegisterText.text = "Password Does Not Match!";
        }
        else
        {
            //Call the Firebase auth signin function passing the email and password
            var RegisterTask = auth.CreateUserWithEmailAndPasswordAsync(_email, _password);
            //Wait until the task completes
            yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);

            if (RegisterTask.Exception != null)
            {
                //If there are errors handle them
                Debug.LogWarning(message: $"Failed to register task with {RegisterTask.Exception}");
                FirebaseException firebaseEx = RegisterTask.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                string message = "Register Failed!";
                switch (errorCode)
                {
                    case AuthError.MissingEmail:
                        message = "Missing Email";
                        break;
                    case AuthError.MissingPassword:
                        message = "Missing Password";
                        break;
                    case AuthError.WeakPassword:
                        message = "Weak Password";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        message = "Email Already In Use";
                        break;
                }
                warningRegisterText.text = message;
            }
            else
            {
                //User has now been created
                //Now get the result
                User = RegisterTask.Result;

                if (User != null)
                {
                    //Create a user profile and set the username
                    UserProfile profile = new UserProfile { DisplayName = _username };

                    //Call the Firebase auth update user profile function passing the profile with the username
                    var ProfileTask = User.UpdateUserProfileAsync(profile);
                    //Wait until the task completes
                    yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

                    if (ProfileTask.Exception != null)
                    {
                        //If there are errors handle them
                        Debug.LogWarning(message: $"Failed to register task with {ProfileTask.Exception}");
                        FirebaseException firebaseEx = ProfileTask.Exception.GetBaseException() as FirebaseException;
                        AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                        warningRegisterText.text = "Username Set Failed!";
                    }
                    else
                    {
                        //Username is now set
                        //Now return to login screen
                        //UIManager.instance.LoginScreen();                        
                        warningRegisterText.text = "";
                        StartCoroutine(SendVerificationEmail());
                        StartCoroutine(UpdateUsernameDatabase(_username));
                    }
                }


            }
        }
    }

    private IEnumerator UpdateUsernameDatabase(string _username)
    {
        UserData data = new UserData();
        data.username = _username;
        data.score = 0;

        string json = JsonUtility.ToJson(data);
        var DBTask = DBreference.Child("users").Child(User.UserId).SetRawJsonValueAsync(json);

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            PlayerPrefs.SetInt("score", data.score);
        }
    }

    public void SetScore(int _score)
    {
        StartCoroutine(UpdateScore(_score));
    }
    public IEnumerator UpdateScore(int _score)
    {
        var DBTask = DBreference.Child("users").Child(User.UserId).Child("score").SetValueAsync(_score);

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            Debug.Log("new best score set at data base");
        }
    }

    public void GetScoreboardValues()
    {
        StartCoroutine(LoadScoreboardData());
    }
    private IEnumerator LoadScoreboardData()
    {
        //Get all the users data ordered by kills amount
        var DBTask = DBreference.Child("users").OrderByChild("score").GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            //Data has been retrieved
            DataSnapshot snapshot = DBTask.Result;

            //Loop through every users UID
            int a = 0;
            foreach (DataSnapshot childSnapshot in snapshot.Children.Reverse<DataSnapshot>())
            {
                string username = childSnapshot.Child("username").Value.ToString();
                int score = int.Parse(childSnapshot.Child("score").Value.ToString());

                texts.GetChild(a).gameObject.GetComponent<Text>().text = (a + 1) + ". " + username + ":   " + score;

                a++;
                if (a >= 4)
                    break;
            }
        }
    }
    IEnumerator SendVerificationEmail()
    {
        if (User != null)
        {
            var emailtask = User.SendEmailVerificationAsync();
            yield return new WaitUntil(predicate: () => emailtask.IsCompleted);

            if (emailtask.Exception != null)
            {
                FirebaseException firebaseException = (FirebaseException)emailtask.Exception.GetBaseException();
                AuthError error = (AuthError)firebaseException.ErrorCode;

                string output = "Unknown Error. Please Try Again";

                switch (error)//In case of any error
                {
                    case AuthError.Cancelled:
                        output = "Verification Task was cancellled";
                        break;
                    case AuthError.InvalidRecipientEmail:
                        output = "Invalid Email";
                        break;
                    case AuthError.TooManyRequests:
                        output = "To Many Requestes";
                        break;
                }
            }
            else
            {
                UIManager.instance.VerfyScreen();
            }
        }
    }
    public void ResetPasswordButton()
    {
        string email = emailLoginField.text;
        StartCoroutine(SendEmailRecovery(email));
    }

    IEnumerator SendEmailRecovery(string email)
    {
        var resetTask = auth.SendPasswordResetEmailAsync(email);

        yield return new WaitUntil(() => resetTask.IsCompleted);

        if (resetTask.Exception != null)
        {
            Debug.LogError("SendPasswordResetEmailAsync encountered an error: " + resetTask.Exception);
        }
        else
        {
            UIManager.instance.VerfyScreen();
        }
    }
    //--------------------------------------------------------------------------------
    public void UpdateStatus(bool online)
    {
        if (online)
            StartCoroutine(SetOnline());
        else
            StartCoroutine(SetOffline());
    }
    IEnumerator SetOnline()
    {
        var DBTask = DBreference.Child("onlineUsers").Child(User.UserId).SetValueAsync(User.DisplayName.ToString());

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            Debug.Log("now you are online");
        }
    }
    IEnumerator SetOffline()
    {
        var DBTask = DBreference.Child("onlineUsers").Child(User.UserId).RemoveValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            Debug.Log("now you are offline");
        }
    }

    void LoadOnlineUser(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        else
        {
            for (int i = 0; i < onlineParent.childCount; i++)
            {
                Destroy(onlineParent.GetChild(i).gameObject);
            }

            Dictionary<string, object> users = (Dictionary<string, object>)args.Snapshot.Value;

            foreach (var item in users)
            {
                Debug.Log(item.Value + " is online");
                if (item.Key == User.UserId)
                {
                    GameObject a = Instantiate(online, onlineParent);
                    a.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = item.Value.ToString();
                    a.transform.GetChild(1).gameObject.SetActive(false);
                    a.name = item.Key.ToString();
                }
                else
                {
                    GameObject a = Instantiate(online, onlineParent);
                    for (int i = 0; i < friendsParent.childCount; i++)
                    {
                        if (friendsParent.GetChild(i).name == item.Key)
                        {
                            a.transform.GetChild(1).gameObject.SetActive(false);
                        }
                    }
                    a.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = item.Value.ToString();
                    a.name = item.Key.ToString();
                }
            }
        }
    }

    void LoadFriends(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        else if (args.Snapshot.Value != null)
        {
            for (int i = 0; i < friendsParent.childCount; i++)
            {
                Destroy(friendsParent.GetChild(i).gameObject);
            }
            Dictionary<string, object> friends_ = (Dictionary<string, object>)args.Snapshot.Value;
            foreach (var item in friends_)
            {
                GameObject a = Instantiate(friend, friendsParent);
                a.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = item.Value.ToString();
                a.name = item.Key.ToString();
            }
        }
        else
        {
            Debug.Log("still null");
            for (int i = 0; i < friendsParent.childCount; i++)
            {
                Destroy(friendsParent.GetChild(i).gameObject);
            }
        }
    }

    void LoadRequest(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        else if (args.Snapshot.Value != null)
        {
            for (int i = 0; i < requestParent.childCount; i++)
            {
                Destroy(requestParent.GetChild(i).gameObject);
            }

            Dictionary<string, object> request_ = (Dictionary<string, object>)args.Snapshot.Value;

            foreach (var item in request_)
            {
                GameObject a = Instantiate(request, requestParent);
                a.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = item.Value.ToString();
                a.name = item.Key.ToString();
            }
        }
        else
        {
            for (int i = 0; i < requestParent.childCount; i++)
            {
                Destroy(requestParent.GetChild(i).gameObject);
            }
        }
    }

    //find friends
    
    public void SerchButton()
    {
        StartCoroutine(JointRoom(number));
    }
    public void JoinButton()
    {
        if (RoomField.text != null)
        {
            number = int.Parse(RoomField.text);
        }
        StartCoroutine(JointRoom(number));
    }
    IEnumerator JointRoom(int number)
    {
        numberText.text = number.ToString();    
        var DBTask = DBreference.Child("rooms").Child("room" + number).Child(User.UserId).SetValueAsync(User.DisplayName);

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            //prender sala ui
        }
    }
   
    public void LeaveButton()
    {
        StartCoroutine(Leave(number));
    }
    IEnumerator Leave(int number)
    {
        var DBTask = DBreference.Child("rooms").Child("room" + number).Child(User.UserId).RemoveValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            Debug.Log("out of queue");
            //boton de salir apaga
        }
    }
    public void NextButton()
    {
        StartCoroutine(Next());
    }
    IEnumerator Next()
    {
        var DBTask = DBreference.Child("rooms").GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            Dictionary<string, Dictionary<string, object>> rooms_ = (Dictionary<string, Dictionary<string, object>>)DBTask.Result.Value;
            for (int i = number; i <= 20; i++)
            {
                if (rooms_["room" + i].Count > 0)
                {
                    number = i;
                    break;
                }
                if (i == 20)
                {
                    number = 0;
                    break;
                }
            }
            StartCoroutine(JointRoom(number));
        }
    }
    void LoadRoom(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        else if (args.Snapshot.Value != null)
        {
            for (int i = 0; i < roomUserParent.childCount; i++)
            {
                Destroy(roomUserParent.GetChild(i).gameObject);
            }

            Dictionary<string, object> users = (Dictionary<string, object>)args.Snapshot.Value;

            foreach (var item in users)
            {
                Debug.Log(item.Value + " is online");
                if (item.Key == User.UserId)
                {
                    GameObject a = Instantiate(online, roomUserParent);
                    a.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = item.Value.ToString();
                    a.transform.GetChild(1).gameObject.SetActive(false);
                    a.name = item.Key.ToString();
                }
                else
                {
                    GameObject a = Instantiate(online, roomUserParent);
                    for (int i = 0; i < friendsParent.childCount; i++)
                    {
                        if (friendsParent.GetChild(i).name == item.Key)
                        {
                            a.transform.GetChild(1).gameObject.SetActive(false);
                        }
                    }
                    a.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = item.Value.ToString();
                    a.name = item.Key.ToString();
                }
            }
        }
        else
        {
            for (int i = 0; i < roomUserParent.childCount; i++)
            {
                Destroy(roomUserParent.GetChild(i).gameObject);
            }
        }
    }
}
    public class UserData
{
    public string username;
    public int score;
}
