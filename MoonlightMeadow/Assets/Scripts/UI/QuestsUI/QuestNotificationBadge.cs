using UnityEngine;

/// <summary>
/// Shows a notification badge on the menu tab when a quest is accepted,
/// and hides it when the quest tab is opened or the menu is shown.
/// </summary>
public class QuestNotificationBadge : MonoBehaviour
{
    [SerializeField] private GameObject badge;

    private void OnEnable()
    {
        QuestController.OnQuestAccepted += ShowBadge;
        QuestUI.OnQuestTabOpened += HideBadge;
        MenuController.OnMenuOpened += OnMenuOpened;
    }

    private void OnDisable()
    {
        QuestController.OnQuestAccepted -= ShowBadge;
        QuestUI.OnQuestTabOpened -= HideBadge;
        MenuController.OnMenuOpened -= OnMenuOpened;
    }

    private void OnMenuOpened()
    {
        if (QuestUI.Instance != null && QuestUI.Instance.isActiveAndEnabled)
            HideBadge();
    }

    private void ShowBadge() => badge?.SetActive(true);
    private void HideBadge() => badge?.SetActive(false);
}
