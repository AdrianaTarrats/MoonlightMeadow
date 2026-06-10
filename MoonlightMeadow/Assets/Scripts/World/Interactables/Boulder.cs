using UnityEngine;

/// <summary>
/// Spawnable rock that requires a Pickaxe to break over multiple hits.
/// Drops stone items on destruction and shakes on each hit.
/// </summary>
public class Boulder : MonoBehaviour, IInteractable, ISpawnable
{
    public ToolType RequiredTool => ToolType.Pickaxe;
    public int hitsToBreak = 3;
    private int currentHits;
    public GameObject stoneItemPrefab;
    public int stoneAmount = 2;
    public float dropRadius = 0.5f;
    private ShakeEffect shakeEffect;

    void Awake()
    {
        shakeEffect = gameObject.GetComponent<ShakeEffect>() ?? gameObject.AddComponent<ShakeEffect>();
    }

    // Spawnable fields
    public string spawnId;
    public Vector3Int occupiedTile;

    // ISpawnable explicit implementation
    string ISpawnable.spawnId { get => spawnId; set => spawnId = value; }
    Vector3Int ISpawnable.occupiedTile { get => occupiedTile; set => occupiedTile = value; }

    public bool CanInteract()
    {
        return currentHits < hitsToBreak;
    }

    public void Interact()
    {
        ToolType tool = PlayerEquipment.Instance.GetEquippedToolType();

        if (tool != RequiredTool)
            return;

        if (PlayerState.Instance == null || !PlayerState.Instance.TryConsumeEnergy(1))
            return;

        currentHits++;

        SoundEffectManager.Play("BoulderHit", true);
        shakeEffect?.StartShake();

        if (currentHits >= hitsToBreak)
        {
            BreakBoulder();
        }
    }

    void BreakBoulder()
    {
        DropStone();
        Destroy(gameObject);
    }

    void DropStone()
    {
        for (int i = 0; i < stoneAmount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * dropRadius;

            Vector2 spawnPos = (Vector2)transform.position + offset;

            GameObject stone = Instantiate(
                stoneItemPrefab,
                spawnPos,
                Quaternion.identity
            );

            BounceEffect bounce = stone.GetComponent<BounceEffect>();
            if (bounce != null)
            {
                bounce.StartBounce();
            }
        }
    }
}
