using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// A single row in the quest list UI. Highlights text on hover and fires
/// click/hover callbacks supplied by <see cref="QuestUI"/> when interacted with.
/// </summary>
public class QuestEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Quest quest;

    private Action<Quest> onClick;
    private Action onHoverEnter;
    private Action onHoverExit;

    [SerializeField] Color hoverColor = new Color(1f, 0.95f, 0.5f);

    List<TMP_Text> cachedTexts = new List<TMP_Text>();
    List<Color> originalTextColors = new List<Color>();

    public void Setup(Quest quest, Action<Quest> click, Action enter, Action exit)
    {
        this.quest = quest;
        onClick = click;
        onHoverEnter = enter;
        onHoverExit = exit;
    }

    void Awake()
    {
        cachedTexts.Clear();
        originalTextColors.Clear();
        var texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            cachedTexts.Add(t);
            originalTextColors.Add(t.color);
        }
    }

    public void SetHover(bool hover)
    {
        for (int i = 0; i < cachedTexts.Count; i++)
        {
            if (cachedTexts[i] == null) continue;
            cachedTexts[i].color = hover ? hoverColor : originalTextColors[i];
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHover(true);
        onHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHover(false);
        onHoverExit?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke(quest);
    }
}