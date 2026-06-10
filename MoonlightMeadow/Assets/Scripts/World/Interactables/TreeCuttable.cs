using UnityEngine;

/// <summary>
/// Spawnable tree that requires an Axe to fell over multiple hits.
/// Drops wood items on destruction and shakes on each hit.
/// </summary>
public class CuttableTree : MonoBehaviour, IInteractable, ISpawnable
{
    public ToolType RequiredTool => ToolType.Axe;

    public int hitsToCut = 5;
    private int currentHits;

    public GameObject woodItemPrefab;
    public int woodAmount = 3;
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
        return currentHits < hitsToCut;
    }

    public void Interact()
    {
        ToolType tool = PlayerEquipment.Instance.GetEquippedToolType();

        if (tool != RequiredTool)
            return;

        if (PlayerState.Instance == null || !PlayerState.Instance.TryConsumeEnergy(1))
            return;

        currentHits++;

        SoundEffectManager.Play("TreeHit", true);
        shakeEffect?.StartShake();

        if (currentHits >= hitsToCut)
        {
            CutTree();
        }
    }

    void CutTree()
    {
        DropWood();
        Destroy(gameObject);
    }

    void DropWood()
    {
        for (int i = 0; i < woodAmount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * dropRadius;

            Vector2 spawnPos = (Vector2)transform.position + offset;

            GameObject wood = Instantiate(
                woodItemPrefab,
                spawnPos,
                Quaternion.identity
            );

            BounceEffect bounce = wood.GetComponent<BounceEffect>();
            if (bounce != null)
            {
                bounce.StartBounce();
            }
        }
    }

}
