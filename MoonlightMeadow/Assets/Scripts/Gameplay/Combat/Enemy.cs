using UnityEngine;

/// <summary>Idle movement pattern used by <see cref="Enemy"/>.</summary>
public enum MovementPattern { Circular, Vertical, Bounce }

/// <summary>
/// Enemy entity that extends <see cref="MagicObject"/> (sprite swaps on magic world toggle),
/// implements <see cref="IInteractable"/> (hit with weapon) and <see cref="ISpawnable"/> (tracked by SpawnController).
/// Moves autonomously, reacts to hits, and drops items on death.
/// </summary>
public class Enemy : MagicObject, IInteractable, ISpawnable
{
    public EnemyData enemyData;
    // Spawnable fields
    public string spawnId;
    public Vector3Int occupiedTile;
    public ToolType RequiredTool => ToolType.Weapon;

    private int currentHits;
    private ShakeEffect shakeEffect;
    private Animator animator;

    private float instanceMovementSpeed;
    private float instanceMovementRange;
    private float movementTimer = 0f;
    private float circularAngle;

    private Vector3 startPosition;

    void Awake()
    {
        shakeEffect = gameObject.GetComponent<ShakeEffect>() ?? gameObject.AddComponent<ShakeEffect>();
        animator = GetComponent<Animator>();
        startPosition = transform.position;

        circularAngle = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        // Smooth idle movement when alive
        if (currentHits < enemyData.hitsToKill)
        {
            UpdateIdleMovement();
        }
    }

    void Start()
    {
        if (enemyData == null)
            return;

        // Apply sprite from enemy data
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && enemyData.enemySprite != null)
        {
            spriteRenderer.sprite = enemyData.enemySprite;
        }

        // Re-sync start position in case it was spawned at a different location
        startPosition = transform.position;

        instanceMovementSpeed = Mathf.Max(0.01f, enemyData.movementSpeed + Random.Range(-enemyData.randomSpeedVariation, enemyData.randomSpeedVariation));
        instanceMovementRange = Mathf.Max(0f, enemyData.movementRange + Random.Range(-enemyData.randomRangeVariation, enemyData.randomRangeVariation));
    }

    // ISpawnable explicit implementation
    string ISpawnable.spawnId { get => spawnId; set => spawnId = value; }
    Vector3Int ISpawnable.occupiedTile { get => occupiedTile; set => occupiedTile = value; }

    public bool CanInteract()
    {
        return currentHits < enemyData.hitsToKill;
    }

    public void Interact()
    {
        ToolType tool = PlayerEquipment.Instance.GetEquippedToolType();

        if (tool != RequiredTool)
            return;

        if (PlayerState.Instance == null || !PlayerState.Instance.TryConsumeEnergy(1))
            return;

        currentHits++;

        if (currentHits < enemyData.hitsToKill)
        {
            SoundEffectManager.Play("EnemyHit", true);
        } 

        shakeEffect?.StartShake();

        // Chance to hurt the player
        if (Random.value < enemyData.chanceToHurtPlayer)
            PlayerState.Instance?.TakeDamage(enemyData.damageToPlayer);

        if (currentHits >= enemyData.hitsToKill)
        {
            KillEnemy();
        }
    }

    private void UpdateIdleMovement()
    {
        float animX = 0f, animY = 0f;

        switch (enemyData.movementPattern)
        {
            case MovementPattern.Circular:
                circularAngle += instanceMovementSpeed * Time.deltaTime;
                float cx = Mathf.Cos(circularAngle) * instanceMovementRange;
                float cy = Mathf.Sin(circularAngle) * instanceMovementRange;
                transform.position = startPosition + new Vector3(cx, cy, 0f);
                animX = -Mathf.Sin(circularAngle);
                animY =  Mathf.Cos(circularAngle);
                break;

            case MovementPattern.Vertical:
                movementTimer += instanceMovementSpeed * Time.deltaTime;
                float vy = Mathf.Sin(movementTimer) * instanceMovementRange;
                transform.position = startPosition + new Vector3(0f, vy, 0f);
                animY = Mathf.Cos(movementTimer);
                break;

            case MovementPattern.Bounce:
                movementTimer += instanceMovementSpeed * Time.deltaTime;
                float by = Mathf.Abs(Mathf.Sin(movementTimer)) * instanceMovementRange;
                transform.position = startPosition + new Vector3(0f, by, 0f);
                animY = Mathf.Cos(movementTimer) >= 0f ? 1f : -1f;
                break;
        }

        if (animator != null)
        {
            animator.SetFloat("InputX", animX);
            animator.SetFloat("InputY", animY);
        }
    }

    void KillEnemy()
    {
        SoundEffectManager.Play("EnemyDeath");
        DropItem();
        Destroy(gameObject);
    }

    void DropItem()
    {
        for (int i = 0; i < enemyData.dropAmount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * enemyData.dropRadius;
            Vector2 spawnPos = (Vector2)transform.position + offset;

            GameObject item = Instantiate(
                enemyData.dropItemPrefab,
                spawnPos,
                Quaternion.identity
            );

            BounceEffect bounce = item.GetComponent<BounceEffect>();
            if (bounce != null)
            {
                bounce.StartBounce();
            }
        }
    }
}
