using UnityEngine;
using UnityEngine.EventSystems;

public class Shop : MonoBehaviour, IInteractable
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        CursorManager.Instance.ApplyCursor(CursorType.Hand);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CursorManager.Instance.ApplyCursor(CursorType.Normal);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        print("Clicked on the shop");
    }
}