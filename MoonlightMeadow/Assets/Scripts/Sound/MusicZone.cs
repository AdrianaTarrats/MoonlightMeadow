using UnityEngine;

[RequireComponent(typeof(Collider2D))]
/// <summary>
/// Trigger zone that starts a specific music track when the player enters
/// and reverts to the default track when they leave.
/// </summary>
public class MusicZone : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    private Collider2D zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Play();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayDefault();
    }

    // Called by SaveController after loading to start music for the zone the player is already in.
    public bool ContainsPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null || zoneCollider == null) return false;
        return zoneCollider.OverlapPoint(player.transform.position);
    }

    public void Play()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayTrack(music, volume);
    }
}
