using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Singleton that shows temporary item-pickup notification popups (icon + name)
/// with a fade-out coroutine. Caps the number of simultaneous popups.
/// </summary>
public class ItemPickupUIController : MonoBehaviour
{
    public static ItemPickupUIController Instance { get; private set; }

    public GameObject popupPrefab;
    public int maxPopups = 5;
    public float popupDuration = 3f;
    private readonly Queue<GameObject> activePopups = new();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowItemPickup(string itemName, Sprite itemIcon)
    {
        GameObject newPopup = Instantiate(popupPrefab, transform);
        newPopup.GetComponentInChildren<TMP_Text>().text = itemName;
        Image itemImage = newPopup.transform.Find("ItemIcon")?.GetComponent<Image>();
        if(itemImage != null)        
        {
            itemImage.sprite = itemIcon;
        }
        activePopups.Enqueue(newPopup);

        if(activePopups.Count > maxPopups)
        {
            Destroy(activePopups.Dequeue());
        }

        StartCoroutine(FadeOutAndDestroy(newPopup));
    }

    private IEnumerator FadeOutAndDestroy(GameObject popup)
    {
        yield return new WaitForSeconds(popupDuration);
        if(popup == null) yield break;

        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        for(float timePassed = 0; timePassed < 1f; timePassed += Time.deltaTime)
        {
            if(popup == null) yield break;
            canvasGroup.alpha = 1f - timePassed;
            yield return null;
        }

        Destroy(popup);
    }
}
