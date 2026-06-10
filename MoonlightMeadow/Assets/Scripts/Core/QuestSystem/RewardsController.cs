using UnityEngine;

/// <summary>
/// Singleton that distributes quest rewards (items and gold) to the player
/// when a quest is completed. Items that do not fit in the inventory are dropped in the world.
/// </summary>
public class RewardsController : MonoBehaviour
{
    public static RewardsController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void GiveQuestRewards(Quest quest)
    {
        if (quest.rewards == null) return;

        foreach (QuestReward reward in quest.rewards)
        {
            switch (reward.type)
            {
                case RewardType.Item:
                    GiveItemReward(reward);
                    break;
                case RewardType.Gold:
                    CurrencyController.Instance.AddGold(reward.amount);
                    break;
            }
        }
    }

    public void GiveItemReward(QuestReward reward)
    {
        GameObject itemPrefab = FindAnyObjectByType<ItemDictionary>()?.GetItemPrefabByID(reward.rewardId);

        if (reward.rewardId <= 0 || reward.amount <= 0) return;

        if(!InventoryController.Instance.AddItemByID(reward.rewardId, reward.amount))
        {
            GameObject dropItem = Instantiate(itemPrefab, transform.position + Vector3.down * 2f, Quaternion.identity);
            dropItem.GetComponent<BounceEffect>()?.StartBounce();
        }
        else
        {
            itemPrefab.GetComponent<Item>()?.PickupPopup();
        }
    }
}
