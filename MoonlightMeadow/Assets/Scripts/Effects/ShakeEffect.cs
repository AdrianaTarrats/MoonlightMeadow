using System.Collections;
using UnityEngine;

/// <summary>
/// Coroutine-based horizontal shake effect. Plays a fixed number of left-right oscillations
/// on the GameObject's transform, then returns it to the original position.
/// Typically used when interactable objects are hit (boulders, trees).
/// </summary>
public class ShakeEffect : MonoBehaviour
{
    public float shakeMagnitude = 0.2f;
    public float shakeDuration = 0.4f;
    public int shakeCount = 2;

    /// <summary>Starts the shake animation coroutine.</summary>
    public void StartShake()
    {
        StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator ShakeCoroutine()
    {
        Vector3 startPos = transform.position;

        for (int i = 0; i < shakeCount; i++)
        {
            // Movimiento a la derecha
            Vector3 targetPos = startPos + Vector3.right * shakeMagnitude;
            float elapsed = 0f;
            while (elapsed < shakeDuration / 2f)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / (shakeDuration / 2f));
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Movimiento a la izquierda
            targetPos = startPos + Vector3.left * shakeMagnitude;
            elapsed = 0f;
            while (elapsed < shakeDuration / 2f)
            {
                transform.position = Vector3.Lerp(startPos + Vector3.right * shakeMagnitude, targetPos, elapsed / (shakeDuration / 2f));
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        transform.position = startPos;
    }
}
