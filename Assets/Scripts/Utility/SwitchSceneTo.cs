using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchSceneTo : MonoBehaviour
{
    
    /// <summary>
    /// Switch to the scene with the given name.
    /// Meant to be used for button clicks.
    /// </summary>
    public void GoToScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }

}