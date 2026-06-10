using System.Collections;
using UnityEngine;

/// <summary>
/// Coroutine-based effect that makes a GameObject bounce upward with decreasing height and duration on each repeat.
/// Typically used when items are dropped into the world.
/// </summary>
public class BounceEffect : MonoBehaviour
{
    public float bounceHeight = 0.3f;
    public float bounceDuration = 0.4f;
    public int bounceCount = 3;

    /// <summary>Starts the bounce animation coroutine.</summary>
    public void StartBounce()
    {
        StartCoroutine(BounceHandler());
    }

    private IEnumerator BounceHandler()
    {
        Vector3 startPos = transform.position;
        float localHeight = bounceHeight;
        float localDuration = bounceDuration;

        for(int i = 0; i < bounceCount; i++)
        {
            yield return Bounce(transform, startPos, localHeight, localDuration / 2);
            localHeight *= 0.5f; // Reduce height for each bounce
            localDuration *= 0.5f; // Reduce duration for each bounce
        }

        transform.position = startPos;
    }

    private IEnumerator Bounce(Transform objectTransform, Vector3 startPos, float height, float duration)
    {
        Vector3 peak = startPos + Vector3.up * height;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            objectTransform.position = Vector3.Lerp(startPos, peak, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < duration)
        {
            objectTransform.position = Vector3.Lerp(peak, startPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
