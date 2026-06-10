using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles left-click on a hotbar slot UI element and forwards slot selection to <see cref="HotbarController"/>.
/// </summary>
public class HotbarSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    public int slotIndex;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (HotbarController.Instance == null)
            return;

        HotbarController.Instance.SelectSlotFromUI(slotIndex);
    }
}
