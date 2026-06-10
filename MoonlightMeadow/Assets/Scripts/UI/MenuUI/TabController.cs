using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows one tab page at a time by index, tinting inactive tab images grey.</summary>
public class TabController : MonoBehaviour
{
    public Image[] tabImages;
    public GameObject[] pages;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ActivateTab(0);
    }

    public void ActivateTab(int activeTab)
    {
        for(int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null) pages[i].SetActive(false);
            if (tabImages[i] != null) tabImages[i].color = Color.grey;
        }

        if (pages[activeTab] != null) pages[activeTab].SetActive(true);
        if (tabImages[activeTab] != null) tabImages[activeTab].color = Color.white;
    }
}
