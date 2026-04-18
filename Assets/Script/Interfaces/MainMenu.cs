using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Play")]
    [Tooltip("Drag the Play Button here (same as hooking On Click in the Inspector).")]
    [SerializeField] private Button playButton;

    [Tooltip("Must match the scene name in the Project (e.g. Game). Scene must be in File → Build Settings.")]
    [SerializeField] private string gameSceneName = "Game";

    [Tooltip("If enabled, Space fires the Play button the same way as a left click.")]
    [SerializeField] private bool activatePlayWithSpace = true;

    private void Awake()
    {
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(PlayGame);
    }

    private void Update()
    {
        if (!activatePlayWithSpace || playButton == null || !playButton.interactable)
            return;
        if (Input.GetKeyDown(KeyCode.Space))
            playButton.onClick.Invoke();
    }

    /// <summary>Can stay public if you also wire it from the Button On Click () list.</summary>
    public void PlayGame()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("MainMenu: Assign Game Scene Name.");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }
}
