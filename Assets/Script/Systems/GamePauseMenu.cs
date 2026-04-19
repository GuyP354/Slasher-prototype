using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Escape: open options + pause / close + resume. Arrow keys / WASD change focus.
/// EventSystem + Selectable Color Tint drive Highlighted (mouse hover) and Selected (keyboard/gamepad focus).
/// MenuFocusRelay syncs mouse position with keyboard focus index so they don't fight each other.
/// </summary>
[DefaultExecutionOrder(-200)]
public class GamePauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("UI (auto-found under child \"Options Menu\" if left empty)")]
    [SerializeField] private GameObject optionsMenuRoot;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button backButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [Header("Slider")]
    [SerializeField] private float sliderKeyboardStep = 0.02f;

    private int _focusIndex;

    private void Awake()
    {
        ResolveReferences();
        ConfigureMenuSelectables();
        RegisterPointerFocusRelays();

        if (volumeSlider != null)
        {
            volumeSlider.value = Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1f));
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            OnVolumeChanged(volumeSlider.value);
        }

        if (backButton != null)
            backButton.onClick.AddListener(ResumeGame);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (optionsMenuRoot != null)
            optionsMenuRoot.SetActive(false);

        IsPaused = false;
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (backButton != null)
            backButton.onClick.RemoveListener(ResumeGame);
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
    }

    private void ResolveReferences()
    {
        if (optionsMenuRoot == null)
        {
            Transform t = transform.Find("Options Menu");
            if (t != null) optionsMenuRoot = t.gameObject;
        }

        if (optionsMenuRoot == null) return;

        if (volumeSlider == null)
            volumeSlider = optionsMenuRoot.transform.Find("Slider")?.GetComponent<Slider>();
        if (backButton == null)
            backButton = optionsMenuRoot.transform.Find("Back Button")?.GetComponent<Button>();
        if (mainMenuButton == null)
            mainMenuButton = optionsMenuRoot.transform.Find("Main Menu Button")?.GetComponent<Button>();
    }

    /// <summary>Keyboard/gamepad only — no automatic UI navigation stealing arrow keys.</summary>
    private void ConfigureMenuSelectables()
    {
        var none = new Navigation { mode = Navigation.Mode.None };
        if (volumeSlider != null) volumeSlider.navigation = none;
        if (backButton != null) backButton.navigation = none;
        if (mainMenuButton != null) mainMenuButton.navigation = none;
    }

    /// <summary>
    /// Pointer hover/press on any raycast target for that row updates focus so EventSystem colors match the mouse.
    /// </summary>
    public void NotifyPointerFocus(int index)
    {
        if (!IsPaused || optionsMenuRoot == null || !optionsMenuRoot.activeSelf) return;
        _focusIndex = index;
        ApplyUISelection();
    }

    private void RegisterPointerFocusRelays()
    {
        RegisterRelayChain(volumeSlider != null ? volumeSlider.gameObject : null, 0);
        RegisterRelayChain(backButton != null ? backButton.gameObject : null, 1);
        RegisterRelayChain(mainMenuButton != null ? mainMenuButton.gameObject : null, 2);
    }

    private void RegisterRelayChain(GameObject root, int index)
    {
        if (root == null) return;

        foreach (Graphic g in root.GetComponentsInChildren<Graphic>(true))
        {
            if (g == null || !g.raycastTarget) continue;
            AddRelay(g.gameObject, index);
        }
    }

    private void AddRelay(GameObject go, int index)
    {
        var existing = go.GetComponents<MenuFocusRelay>();
        foreach (MenuFocusRelay r in existing)
        {
            if (r != null && r.Matches(index))
                return;
        }

        MenuFocusRelay relay = go.AddComponent<MenuFocusRelay>();
        relay.Init(this, index);
    }

    private void OnVolumeChanged(float v)
    {
        AudioListener.volume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("MasterVolume", AudioListener.volume);
    }

    private void Update()
    {
        if (optionsMenuRoot == null) return;

        if (MenuEscapePressed())
        {
            if (optionsMenuRoot.activeSelf)
                ResumeGame();
            else
                OpenMenu();
            return;
        }

        if (!optionsMenuRoot.activeSelf) return;

        if (MenuNavigateDownPressed())
        {
            _focusIndex = (_focusIndex + 1) % 3;
            ApplyUISelection();
        }
        else if (MenuNavigateUpPressed())
        {
            _focusIndex = (_focusIndex + 2) % 3;
            ApplyUISelection();
        }

        if (_focusIndex == 0 && volumeSlider != null)
        {
            float v = volumeSlider.value;
            if (MenuSliderLeftHeld())
                v -= sliderKeyboardStep;
            if (MenuSliderRightHeld())
                v += sliderKeyboardStep;
            v = Mathf.Clamp01(v);
            if (!Mathf.Approximately(v, volumeSlider.value))
                volumeSlider.value = v;
        }

        if (MenuSubmitPressed())
            SubmitCurrentSelection();
    }

    private void ApplyUISelection()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return;

        GameObject go = GetFocusedGameObject();
        if (go != null)
            es.SetSelectedGameObject(go);
    }

    private GameObject GetFocusedGameObject()
    {
        switch (_focusIndex)
        {
            case 0: return volumeSlider != null ? volumeSlider.gameObject : null;
            case 1: return backButton != null ? backButton.gameObject : null;
            case 2: return mainMenuButton != null ? mainMenuButton.gameObject : null;
            default: return null;
        }
    }

    private static void ClearUISelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private static bool HasKeyboard()
    {
        return Keyboard.current != null;
    }

    private static bool MenuEscapePressed()
    {
        if (HasKeyboard() && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
        return Input.GetKeyDown(KeyCode.Escape);
    }

    private static bool MenuNavigateUpPressed()
    {
        if (HasKeyboard())
        {
            var k = Keyboard.current;
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)
                return true;
        }

        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
    }

    private static bool MenuNavigateDownPressed()
    {
        if (HasKeyboard())
        {
            var k = Keyboard.current;
            if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)
                return true;
        }

        return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
    }

    private static bool MenuSliderLeftHeld()
    {
        if (HasKeyboard())
        {
            var k = Keyboard.current;
            if (k.leftArrowKey.isPressed || k.aKey.isPressed)
                return true;
        }

        return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
    }

    private static bool MenuSliderRightHeld()
    {
        if (HasKeyboard())
        {
            var k = Keyboard.current;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed)
                return true;
        }

        return Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
    }

    private static bool MenuSubmitPressed()
    {
        if (HasKeyboard())
        {
            var k = Keyboard.current;
            if (k.spaceKey.wasPressedThisFrame)
                return true;
        }

        return Input.GetKeyDown(KeyCode.Space);
    }

    private static void SubmitCurrentSelection()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return;

        GameObject selected = es.currentSelectedGameObject;
        if (selected == null) return;

        ExecuteEvents.Execute(selected, new BaseEventData(es), ExecuteEvents.submitHandler);
    }

    public void OpenMenu()
    {
        if (optionsMenuRoot == null) return;
        optionsMenuRoot.SetActive(true);
        Time.timeScale = 0f;
        IsPaused = true;
        _focusIndex = 0;
        ApplyUISelection();
    }

    public void ResumeGame()
    {
        if (optionsMenuRoot == null) return;
        ClearUISelection();
        optionsMenuRoot.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
    }

    public void GoToMainMenu()
    {
        ClearUISelection();
        Time.timeScale = 1f;
        IsPaused = false;
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("GamePauseMenu: Assign main menu scene name.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
