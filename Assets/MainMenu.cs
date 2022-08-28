using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        transform.parent.parent.transform.GetChild(1).gameObject.SetActive(true);
        transform.parent.parent.transform.GetChild(3).gameObject.SetActive(true);
        transform.parent.transform.gameObject.SetActive(false);
        GameObject.Find("GameScene").GetComponent<GameControl>().mapNumber = 0;
    }

    public void PlayMap2()
    {
        transform.parent.parent.transform.GetChild(1).gameObject.SetActive(true);
        transform.parent.parent.transform.GetChild(4).gameObject.SetActive(true);
        transform.parent.transform.gameObject.SetActive(false);
        GameObject.Find("GameScene").GetComponent<GameControl>().mapNumber = 1;
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
