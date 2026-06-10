using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Swaps the hardware cursor between a normal texture and an interact texture
/// based on whether an <see cref="IInteractable"/> is under the mouse and within range.
/// </summary>
public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;
    [SerializeField] private LayerMask interactableLayer;

    public Texture2D normalCursor;
    public Texture2D interactCursor;

    public Texture2D interactFadedCursor;
    //public Texture2D pickupFadedCursor;
    public Vector2 hotspot = Vector2.zero;

    public Transform player;
    public float interactRange = 2f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetNormal();
    }

    void Update()
    {
        UpdateCursor();
    }

    void UpdateCursor()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = Mathf.Abs(Camera.main.transform.position.z);

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, interactableLayer);

        if (hit != null)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();

            if (interactable != null && interactable.CanInteract())
            {
                float distance = Vector2.Distance(player.position, hit.transform.position);

                if (distance <= interactRange)
                    Cursor.SetCursor(interactCursor, hotspot, CursorMode.Auto);
                else
                    Cursor.SetCursor(interactFadedCursor, hotspot, CursorMode.Auto);

                return;
            }
        }

        SetNormal();
    }

    public void SetNormal()
    {
        Cursor.SetCursor(normalCursor, hotspot, CursorMode.Auto);
    }

    public void SetInteract()
    {
        Cursor.SetCursor(interactCursor, hotspot, CursorMode.Auto);
    }
}
