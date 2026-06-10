using UnityEngine;

/// <summary>
/// Sets the <see cref="SpriteRenderer"/> sorting order based on the object's world-Y
/// so that sprites lower on screen render in front of those higher up.
/// Optionally runs every LateUpdate for moving objects (<c>continuous</c> mode).
/// </summary>
public class YSortByPosition : MonoBehaviour
{
    [SerializeField] private int sortingOffset = 0;
    [SerializeField] private int sortingMultiplier = 100;
    [SerializeField] private bool continuous = false;
    [Tooltip("If set, this transform's Y is used for sorting instead of this object's Y. " +
             "Place an empty child at the sprite's base (roots/feet) and assign it here.")]
    [SerializeField] private Transform sortingPoint;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        // Called in Start rather than Awake so sprite bounds are guaranteed to be ready.
        ApplySorting();
    }

    private void LateUpdate()
    {
        if (continuous)
        {
            ApplySorting();
        }
    }

    public void ApplySorting()
    {
        if (sr == null || sr.sprite == null)
            return;

        float y;
        if (sortingPoint != null)
            y = sortingPoint.position.y;
        else
            y = sr.bounds.min.y; // bottom of sprite in world space — correct regardless of pivot point

        sr.sortingOrder = sortingOffset + Mathf.RoundToInt(-y * sortingMultiplier);
    }
}
