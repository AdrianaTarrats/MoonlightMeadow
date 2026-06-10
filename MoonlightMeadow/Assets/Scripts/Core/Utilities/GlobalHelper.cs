using UnityEngine;

/// <summary>Static utility methods shared across multiple systems.</summary>
public static class GlobalHelper
{
    /// <summary>
    /// Generates a scene-stable unique ID for a GameObject based on its scene name and world position.
    /// </summary>
    /// <param name="obj">The GameObject to generate an ID for.</param>
    /// <returns>A string of the form "SceneName_x_y".</returns>
    public static string GenerateUniqueID(GameObject obj)
    {
        return $"{obj.scene.name}_{obj.transform.position.x}_{obj.transform.position.y}";
    }
}
