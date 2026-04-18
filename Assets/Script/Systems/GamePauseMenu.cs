using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Escape: open options + pause / close + resume. Arrow keys move focus (slider → back → main menu).
/// Left/Right adjusts slider. Space activates focused back (resume) or main menu (load scene).
/// </summary>
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
    private Graphic _sliderGraphic;
    private Graphic _backGraphic;
    private Graphic _mainGraphic;

    private void Awake()
    {
        ResolveReferences();
        CacheGraphics();

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
            var t = transform.Find("Options Menu");
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

    private void CacheGraphics()
    {
        if (volumeSlider != null && volumeSlider.targetGraphic != null)
            _sliderGraphic = volumeSlider.targetGraphic;
        if (backButton != null && backButton.targetGraphic != null)
            _backGraphic = backButton.targetGraphic;
        if (mainMenuButton != null && mainMenuButton.targetGraphic != null)
            _mainGraphic = mainMenuButton.targetGraphic;
    }

    private void OnVolumeChanged(float v)
    {
        AudioListener.volume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("MasterVolume", AudioListener.volume);
    }

    private void Update()
    {
        if (optionsMenuRoot == null) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionsMenuRoot.activeSelf)
                ResumeGame();
            else
                OpenMenu();
            return;
        }

        if (!optionsMenuRoot.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            _focusIndex = (_focusIndex + 1) % 3;
            RefreshFocusVisuals();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _focusIndex = (_focusIndex + 2) % 3;
            RefreshFocusVisuals();
        }

        if (_focusIndex == 0 && volumeSlider != null)
        {
            float v = volumeSlider.value;
            if (Input.GetKey(KeyCode.LeftArrow))
                v -= sliderKeyboardStep;
            if (Input.GetKey(KeyCode.RightArrow))
                v += sliderKeyboardStep;
            v = Mathf.Clamp01(v);
            if (!Mathf.Approximately(v, volumeSlider.value))
                volumeSlider.value = v;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_focusIndex == 1)
                ResumeGame();
            else if (_focusIndex == 2)
                GoToMainMenu();
        }
    }

    public void OpenMenu()
    {
        if (optionsMenuRoot == null) return;
        optionsMenuRoot.SetActive(true);
        Time.timeScale = 0f;
        IsPaused = true;
        _focusIndex = 0;
        RefreshFocusVisuals();
    }

    public void ResumeGame()
    {
        if (optionsMenuRoot == null) return;
        optionsMenuRoot.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("GamePauseMenu: Assign main menu scene name.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void RefreshFocusVisuals()
    {
        if (volumeSlider != null && _sliderGraphic != null)
        {
            ColorBlock c = volumeSlider.colors;
            _sliderGraphic.color = _focusIndex == 0 ? c.highlightedColor : c.normalColor;
        }

        if (backButton != null && _backGraphic != null)
        {
            ColorBlock c = backButton.colors;
            _backGraphic.color = _focusIndex == 1
                ? c.highlightedColor
                : Color.Lerp(c.normalColor, c.highlightedColor, 0.45f);
        }

        if (mainMenuButton != null && _mainGraphic != null)
        {
            ColorBlock c = mainMenuButton.colors;
            _mainGraphic.color = _focusIndex == 2
                ? c.highlightedColor
                : Color.Lerp(c.normalColor, c.highlightedColor, 0.45f);
        }
    }
}
