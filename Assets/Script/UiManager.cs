using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class UiManager : MonoBehaviour
{
    float currentTime;
    public PlayerRespawn playerRespawn;

    public Image clearBg;
    public Text deathCountText;
    public Text timeText;

    public Text clearText;

    public Button restartButton;

    public GameObject EndPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        clearBg.enabled = false;
        deathCountText.enabled = false;
        timeText.enabled = false;
        clearText.enabled = false;
        restartButton.gameObject.SetActive(false);

    }


    void Update()
    {

        if (EndPoint.GetComponent<EndPoint>().isEndPointReached)
        {
            clearBg.enabled = true;
            clearText.enabled = true;
            timeText.enabled = true;
            deathCountText.enabled = true;
            deathCountText.text = "Current Deaths: " + playerRespawn.playerCurDeath.ToString();
            timeText.text = "Time: " + currentTime.ToString("F2") + "s";
            restartButton.gameObject.SetActive(true);


            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;


            return;
        }
        else
        {
            currentTime += Time.deltaTime;
        }



    }

    public void RestartGame()
    {
        // 게임 재시작 로직 구현
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }


}
