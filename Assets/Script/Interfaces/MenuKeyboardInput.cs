using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public static class MenuKeyboardInput
{
    public static Slider FindSliderUnder(Transform root, params string[] objectNames)
    {
        if (root == null || objectNames == null || objectNames.Length == 0)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < objectNames.Length; i++)
        {
            string targetName = objectNames[i];
            if (string.IsNullOrEmpty(targetName))
                continue;

            for (int c = 0; c < children.Length; c++)
            {
                Transform child = children[c];
                if (child == null || child.name != targetName)
                    continue;

                Slider slider = child.GetComponent<Slider>();
                if (slider != null)
                    return slider;
            }
        }

        return null;
    }

    public static bool EscapePressed()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;

        return Input.GetKeyDown(KeyCode.Escape);
    }

    public static bool NavigateUpPressed()
    {
        if (Keyboard.current != null)
        {
            var k = Keyboard.current;
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.numpad8Key.wasPressedThisFrame)
                return true;
        }

        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Keypad8);
    }

    public static bool NavigateDownPressed()
    {
        if (Keyboard.current != null)
        {
            var k = Keyboard.current;
            if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame)
                return true;
        }

        return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Keypad2);
    }

    public static bool SliderLeftHeld()
    {
        if (Keyboard.current != null)
        {
            var k = Keyboard.current;
            if (k.leftArrowKey.isPressed || k.aKey.isPressed || k.numpad4Key.isPressed)
                return true;
        }

        return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Keypad4);
    }

    public static bool SliderRightHeld()
    {
        if (Keyboard.current != null)
        {
            var k = Keyboard.current;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed || k.numpad6Key.isPressed)
                return true;
        }

        return Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.Keypad6);
    }

    public static bool SubmitPressed()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;

        return Input.GetKeyDown(KeyCode.Space);
    }

    public static void AdjustSlider(Slider slider, float step)
    {
        if (slider == null)
            return;

        float value = slider.value;
        if (SliderLeftHeld())
            value -= step;
        if (SliderRightHeld())
            value += step;

        slider.value = Mathf.Clamp01(value);
    }
}
