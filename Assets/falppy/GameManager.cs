using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject gameOverCanvas;
    AuthManager authManager;

    // Start is called before the first frame update
    void Start()
    {
        authManager = gameObject.GetComponent<AuthManager>();
        Time.timeScale = 1;
    }

    public void GameOver()
    {
        gameOverCanvas.SetActive(true);
        Time.timeScale = 0;

        if (PlayerPrefs.GetInt("score") < Score.score)
        {
            PlayerPrefs.SetInt("score", Score.score);
            print("new best");

            authManager.SetScore(Score.score);
        }
        print("score: " + PlayerPrefs.GetInt("score"));
    }
    public void Replay()
    {
        SceneManager.LoadScene(1);
    }
}
