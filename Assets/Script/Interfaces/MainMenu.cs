using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Menus")]
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private GameObject mainMenuContainer;
    [SerializeField] private GameObject optionsMenuRoot;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Input")]
    [Tooltip("If enabled, Space triggers Submit on the selected UI control.")]
    [SerializeField] private bool activateSubmitWithSpace = true;

    [Header("Audio")]
    [SerializeField] private Slider volumeSlider;

    private void Awake()
    {
        AutoAssignReferences();
        InitMasterVolume();
        EnsureGlobalSpaceSubmit();

        SetMainMenuVisible(true);
        SetOptionsMenuVisible(false);
        SelectButton(playButton);
        ConfigureMainMenuNavigation();

        RegisterButton(playButton, PlayGame);
        RegisterButton(optionsButton, OpenOptionsMenu);
        RegisterButton(quitButton, QuitGame);
        RegisterButton(mainMenuButton, ReturnToMainMenu);
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);

        UnregisterButton(playButton, PlayGame);
        UnregisterButton(optionsButton, OpenOptionsMenu);
        UnregisterButton(quitButton, QuitGame);
        UnregisterButton(mainMenuButton, ReturnToMainMenu);
    }

    private void Update()
    {
        if (NavigationInputPressed())
            RestoreSelectionIfLost();
    }

    public void PlayGame()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("MainMenu: Assign Game Scene Name.");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptionsMenu()
    {
        SetOptionsMenuVisible(true);
        SetMainMenuVisible(false);
        SelectButton(mainMenuButton);
    }

    public void ReturnToMainMenu()
    {
        SetMainMenuVisible(true);
        SetOptionsMenuVisible(false);
        SelectButton(playButton);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void RegisterButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private static void UnregisterButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }

    private static void SelectButton(Button button)
    {
        if (button != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void AutoAssignReferences()
    {
        if (mainMenuContainer == null) mainMenuContainer = FindObjectByName("MainMenu");
        if (mainMenuRoot == null) mainMenuRoot = FindObjectByName("Panel");
        if (optionsMenuRoot == null) optionsMenuRoot = FindObjectByName("Options Menu");

        if (playButton == null) playButton = FindButtonByName("Play Button");
        if (optionsButton == null) optionsButton = FindButtonByName("Options Button");
        if (quitButton == null) quitButton = FindButtonByName("Quit Button");
        if (mainMenuButton == null) mainMenuButton = FindButtonByName("Main Menu Button");

        if (volumeSlider == null && optionsMenuRoot != null)
            volumeSlider = optionsMenuRoot.transform.Find("Slider")?.GetComponent<Slider>();
    }

    private void InitMasterVolume()
    {
        float saved = Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1f));
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(saved);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        OnVolumeChanged(saved);
    }

    private static void OnVolumeChanged(float value)
    {
        float v = Mathf.Clamp01(value);
        AudioListener.volume = v;
        PlayerPrefs.SetFloat("MasterVolume", v);
    }

    private void SetMainMenuVisible(bool visible)
    {
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(visible);

        if (mainMenuContainer != null)
            mainMenuContainer.SetActive(visible);
    }

    private void SetOptionsMenuVisible(bool visible)
    {
        if (optionsMenuRoot != null)
            optionsMenuRoot.SetActive(visible);
    }

    private static Button FindButtonByName(string objectName)
    {
        GameObject found = FindObjectByName(objectName);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private static GameObject FindObjectByName(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void EnsureGlobalSpaceSubmit()
    {
        if (!Application.isPlaying)
            return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        if (eventSystem.GetComponent<GlobalSpaceSubmit>() == null)
            eventSystem.gameObject.AddComponent<GlobalSpaceSubmit>();
    }

    private void RestoreSelectionIfLost()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected != null && selected.activeInHierarchy)
            return;

        if (optionsMenuRoot != null && optionsMenuRoot.activeInHierarchy)
        {
            SelectButton(mainMenuButton);
            return;
        }

        SelectButton(playButton);
    }

    private static bool NavigationInputPressed()
    {
        return Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetKeyDown(KeyCode.DownArrow) ||
               Input.GetKeyDown(KeyCode.LeftArrow) ||
               Input.GetKeyDown(KeyCode.RightArrow) ||
               Input.GetKeyDown(KeyCode.W) ||
               Input.GetKeyDown(KeyCode.S) ||
               Input.GetKeyDown(KeyCode.A) ||
               Input.GetKeyDown(KeyCode.D);
    }

    private static bool IsSelectionOnButton(GameObject selected, Button button)
    {
        if (selected == null || button == null)
            return false;

        Transform t = selected.transform;
        Transform buttonTransform = button.transform;

        while (t != null)
        {
            if (t == buttonTransform)
                return true;
            t = t.parent;
        }

        return false;
    }

    private void ConfigureMainMenuNavigation()
    {
        ConfigureButtonNavigation(playButton, null, optionsButton);
        ConfigureButtonNavigation(optionsButton, playButton, quitButton);
        ConfigureButtonNavigation(quitButton, optionsButton, null);
    }

    private static void ConfigureButtonNavigation(Button button, Selectable up, Selectable down)
    {
        if (button == null)
            return;

        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnUp = up;
        nav.selectOnDown = down;
        nav.selectOnLeft = null;
        nav.selectOnRight = null;
        button.navigation = nav;
    }
}

public class GlobalSpaceSubmit : MonoBehaviour
{
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected == null)
            return;

        ExecuteEvents.Execute(selected, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
    }
}
