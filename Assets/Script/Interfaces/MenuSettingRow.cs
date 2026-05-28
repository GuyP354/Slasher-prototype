using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a settings row behave like a menu button: keyboard focus + left/right adjusts the linked slider value.
/// </summary>
[DisallowMultipleComponent]
public class MenuSettingRow : MonoBehaviour
{
    public enum SettingKind
    {
        Volume,
        Brightness
    }

    [SerializeField] private SettingKind settingKind = SettingKind.Brightness;
    [SerializeField] private Slider valueSlider;
    [SerializeField] private Button focusButton;
    [SerializeField] private float keyboardStep = 0.02f;

    public SettingKind Kind => settingKind;
    public Slider ValueSlider => valueSlider;
    public Selectable FocusSelectable => focusButton != null ? focusButton : valueSlider;

    public void Configure(SettingKind kind, Slider slider = null, Button focus = null)
    {
        settingKind = kind;
        if (slider != null)
            valueSlider = slider;
        if (focus != null)
            focusButton = focus;

        EnsureComponents();
    }

    private void Awake()
    {
        EnsureComponents();
    }

    private void EnsureComponents()
    {
        if (valueSlider == null)
            valueSlider = GetComponent<Slider>();

        if (focusButton == null)
            focusButton = GetComponent<Button>();

        if (FocusSelectable != null)
        {
            Navigation nav = FocusSelectable.navigation;
            nav.mode = Navigation.Mode.None;
            FocusSelectable.navigation = nav;
        }
    }

    public void LoadSavedValue()
    {
        if (valueSlider == null)
            return;

        float saved = settingKind == SettingKind.Volume
            ? Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1f))
            : GlobalBrightness.GetBrightness();

        valueSlider.SetValueWithoutNotify(saved);
        ApplyValue(saved);
    }

    public void BindValueChanged()
    {
        if (valueSlider == null)
            return;

        valueSlider.onValueChanged.RemoveListener(ApplyValue);
        valueSlider.onValueChanged.AddListener(ApplyValue);
    }

    public void UnbindValueChanged()
    {
        if (valueSlider != null)
            valueSlider.onValueChanged.RemoveListener(ApplyValue);
    }

    public void AdjustWithKeyboard()
    {
        if (valueSlider == null)
            return;

        MenuKeyboardInput.AdjustSlider(valueSlider, keyboardStep);
    }

    private void ApplyValue(float value)
    {
        float clamped = Mathf.Clamp01(value);

        if (settingKind == SettingKind.Volume)
        {
            AudioListener.volume = clamped;
            PlayerPrefs.SetFloat("MasterVolume", clamped);
            PlayerPrefs.Save();
            return;
        }

        GlobalBrightness.SetBrightness(clamped);
    }
}
