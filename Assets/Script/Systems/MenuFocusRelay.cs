using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Forwards pointer hover/press to GamePauseMenu so keyboard focus index matches the mouse.
/// Attach to each raycast target under the options controls (done at runtime for Slider children).
/// </summary>
public class MenuFocusRelay : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    private GamePauseMenu _menu;
    private int _index;

    public void Init(GamePauseMenu menu, int index)
    {
        _menu = menu;
        _index = index;
    }

    public bool Matches(int index)
    {
        return _index == index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Notify();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Notify();
    }

    private void Notify()
    {
        if (_menu != null)
            _menu.NotifyPointerFocus(_index);
    }
}
