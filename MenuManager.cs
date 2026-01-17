using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void Play1vs1()
    {
        SceneManager.LoadScene("Game 1vs1");
    }

    public void Play1vsBOT()
    {
        SceneManager.LoadScene("Game 1vsBOT");
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    public void Exit()
    {
        Application.Quit();
    }
}
