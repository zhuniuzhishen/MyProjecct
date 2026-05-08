using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Button Button_Start; 
    public Button Button_Quit;

    private void Awake()
    {
        // 从游戏内暂停进入主菜单时 timeScale 可能仍为 0
        Time.timeScale = 1f;

        Button_Start = GameObject.Find("Button_Start").GetComponent<Button>();
        Button_Quit = GameObject.Find("Button_Quit").GetComponent<Button>();
        
    }

    // Start is called before the first frame update
    void Start()
    {
        Button_Start.onClick.AddListener(StartGame);   
        
        
        Button_Quit.onClick.AddListener(() =>
        {
            ExitGame();
        });   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    //开始游戏
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
    
    //退出游戏
    public void ExitGame()
    {
        Application.Quit();
    }
    
    
}
