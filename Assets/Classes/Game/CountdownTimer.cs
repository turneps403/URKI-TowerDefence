using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    public string LevelToLoad;
    public float timer = 10f;
    private Text timerSeconds;

    // Use this for initialization
    void Start()
    {
        timerSeconds = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;
        timerSeconds.text = timer.ToString("f0");

        if (timer <= 0)
        {

            GameLoopManager.ClearAllQueues();

            SceneManager.LoadScene(LevelToLoad); // Используем новый метод загрузки сцены
        }
    }
}