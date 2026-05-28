using System.Collections;
using UnityEngine;

/// <summary>
/// Opening tutorial: Flash 1 → 2 → 3 (15s each, fade out), while the Press E gate hint unlocks after a delay.
/// Flash hints ignore proximity; gate interaction (E) works throughout.
/// </summary>
[DefaultExecutionOrder(-50)]
public class GameHintsTutorial : MonoBehaviour
{
    private static GameHintsTutorial instance;

    /// <summary>When no tutorial exists in the scene, gate hints behave normally.</summary>
    public static bool IsGateHintUnlocked => instance == null || instance.gateHintUnlocked;

    [Header("Timing")]
    [SerializeField] private float flashDurationSeconds = 15f;
    [SerializeField] private float fadeOutSeconds = 1f;
    [SerializeField] private float gateHintUnlockDelaySeconds = 40f;

    [Header("Hints (auto-filled from child names if empty)")]
    [SerializeField] private GameObject flash1Visual;
    [SerializeField] private GameObject flash2Visual;
    [SerializeField] private GameObject flash3Visual;
    [SerializeField] private PhaseProximityHintUI gateStartHint;

    private bool gateHintUnlocked;
    private Coroutine sequenceRoutine;

    private void Awake()
    {
        instance = this;
        gateHintUnlocked = false;
        ResolveReferences();
        DisableFlashProximityHints();
        HideAllFlashVisuals();
        HideGateHintVisual();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Start()
    {
        StartCoroutine(UnlockGateHintAfterDelay());
        sequenceRoutine = StartCoroutine(RunOpeningTutorial());
    }

    private void ResolveReferences()
    {
        if (gateStartHint == null)
        {
            PhaseProximityHintUI[] hints = GetComponentsInChildren<PhaseProximityHintUI>(true);
            for (int i = 0; i < hints.Length; i++)
            {
                if (hints[i] != null && hints[i].gameObject.name.Contains("Press E"))
                {
                    gateStartHint = hints[i];
                    break;
                }
            }
        }

        if (flash1Visual == null)
            flash1Visual = ResolveFlashVisual("Flash 1");
        if (flash2Visual == null)
            flash2Visual = ResolveFlashVisual("Flash 2");
        if (flash3Visual == null)
            flash3Visual = ResolveFlashVisual("Flash 3");
    }

    private GameObject ResolveFlashVisual(string flashObjectName)
    {
        Transform flashRoot = transform.Find(flashObjectName);
        if (flashRoot == null)
            return null;

        PhaseProximityHintUI hint = flashRoot.GetComponent<PhaseProximityHintUI>();
        if (hint != null)
            return hint.GetVisualRoot();

        if (flashRoot.childCount > 0)
            return flashRoot.GetChild(0).gameObject;

        return null;
    }

    private void DisableFlashProximityHints()
    {
        DisableHintOnFlash("Flash 1");
        DisableHintOnFlash("Flash 2");
        DisableHintOnFlash("Flash 3");
    }

    private void DisableHintOnFlash(string flashObjectName)
    {
        Transform flashRoot = transform.Find(flashObjectName);
        if (flashRoot == null)
            return;

        PhaseProximityHintUI hint = flashRoot.GetComponent<PhaseProximityHintUI>();
        if (hint != null)
            hint.enabled = false;
    }

    private void HideAllFlashVisuals()
    {
        SetVisualActive(flash1Visual, false);
        SetVisualActive(flash2Visual, false);
        SetVisualActive(flash3Visual, false);
    }

    private void HideGateHintVisual()
    {
        if (gateStartHint != null)
            gateStartHint.ForceHideVisual();
    }

    private IEnumerator RunOpeningTutorial()
    {
        yield return ShowFlashWithFade(flash1Visual);
        yield return ShowFlashWithFade(flash2Visual);
        yield return ShowFlashWithFade(flash3Visual);

        sequenceRoutine = null;
    }

    private IEnumerator UnlockGateHintAfterDelay()
    {
        float delay = Mathf.Max(0f, gateHintUnlockDelaySeconds);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        gateHintUnlocked = true;
    }

    private IEnumerator ShowFlashWithFade(GameObject visual)
    {
        if (visual == null)
        {
            yield return new WaitForSeconds(flashDurationSeconds);
            yield break;
        }

        CanvasGroup group = GetOrAddCanvasGroup(visual);
        visual.SetActive(true);
        group.alpha = 1f;

        float hold = Mathf.Max(0f, flashDurationSeconds - fadeOutSeconds);
        if (hold > 0f)
            yield return new WaitForSeconds(hold);

        float fade = Mathf.Max(0.01f, fadeOutSeconds);
        float elapsed = 0f;
        while (elapsed < fade)
        {
            elapsed += Time.deltaTime;
            group.alpha = 1f - Mathf.Clamp01(elapsed / fade);
            yield return null;
        }

        group.alpha = 0f;
        visual.SetActive(false);
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject visual)
    {
        CanvasGroup group = visual.GetComponent<CanvasGroup>();
        if (group == null)
            group = visual.AddComponent<CanvasGroup>();

        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    private static void SetVisualActive(GameObject visual, bool active)
    {
        if (visual != null)
            visual.SetActive(active);
    }
}
