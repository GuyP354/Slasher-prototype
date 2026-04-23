using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Place on Loose Game Canvas. While this canvas is active (gate health lost), Space or the You Loose Button call <see cref="GoToMainMenu"/>.
/// </summary>
public class YouLooseCanvasMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (SpacePressed())
            GoToMainMenu();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("YouLooseCanvasMenu: Assign main menu scene name.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private static bool SpacePressed()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;

        return Input.GetKeyDown(KeyCode.Space);
    }
}
