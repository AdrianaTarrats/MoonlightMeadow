using System;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Static helper that performs a full map transition: fade-out, teleport player,
/// snap Cinemachine camera, update confiner boundary, then fade-in.
/// Guards against overlapping transitions with an <c>isTransitioning</c> flag.
/// </summary>
public static class TransitionHelper
{
    private static bool isTransitioning;

    /// <summary>
    /// Executes a full scene transition: fade out → teleport → camera warp → confiner update → fade in.
    /// </summary>
    /// <param name="player">The player transform to teleport.</param>
    /// <param name="destination">Target position after the teleport.</param>
    /// <param name="mapBoundary">New Cinemachine confiner boundary to apply after teleporting.</param>
    /// <param name="isIndoor">If true, disables the sunlight during the transition.</param>
    /// <param name="onAfterTeleport">Optional callback executed immediately after the player is moved.</param>
    /// <returns>True if the transition completed; false if one was already in progress.</returns>
    public static async Task<bool> RunTransition(
        Transform player,
        Transform destination,
        PolygonCollider2D mapBoundary,
        bool isIndoor,
        Action onAfterTeleport = null)
    {
        if (isTransitioning)
            return false;

        if (player == null)
            return false;

        if (destination == null)
            return false;

        isTransitioning = true;

        bool wasPausedBeforeTransition = PauseController.IsGamePaused;
        bool pausedByTransition = false;

        if (!wasPausedBeforeTransition)
        {
            PauseController.SetPause(true);
            pausedByTransition = true;
        }

        try
        {
            // Fade out
            if (ScreenFader.Instance != null)
            {
                await ScreenFader.Instance.FadeOut();
            }

            // Grab Cinemachine components (FadeOut may have unpaused the game)
            CinemachineConfiner2D confiner =
                UnityEngine.Object.FindFirstObjectByType<CinemachineConfiner2D>();
            CinemachineCamera cinemachineCamera =
                UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>();

            float savedConfinerDamping = 0f;

            if (confiner != null)
            {
                savedConfinerDamping = confiner.Damping;
                confiner.Damping = 0f;

                // Disable the confiner so it does not push the camera while
                // Cinemachine is still settling after the teleport.
                confiner.enabled = false;
            }

            // Lighting
            LightingController lightingController =
                UnityEngine.Object.FindFirstObjectByType<LightingController>();

            if (lightingController != null && lightingController.sunlight != null)
            {
                lightingController.sunlight.enabled = !isIndoor;
            }

            // Teleport player
            Vector3 previousPosition = player.position;
            player.position = destination.position;
            Vector3 warpDelta = player.position - previousPosition;

            // Tell Cinemachine about the teleport so it does not smoothly pan
            if (warpDelta != Vector3.zero && cinemachineCamera != null)
            {
                cinemachineCamera.OnTargetObjectWarped(player, warpDelta);
            }

            // Extra logic after teleport
            onAfterTeleport?.Invoke();

            // Let Cinemachine settle at the player's exact position with no
            // confiner constraint pushing it around.
            await Awaitable.EndOfFrameAsync();
            await Awaitable.EndOfFrameAsync();

            // Now re-enable the confiner with the new boundary.
            // With Damping=0 it snaps instantly in one LateUpdate.
            if (confiner != null)
            {
                if (mapBoundary != null)
                    confiner.BoundingShape2D = mapBoundary;

                confiner.enabled = true;
                confiner.InvalidateBoundingShapeCache();
            }

            // One frame for the confiner to calculate and apply the constraint
            await Awaitable.EndOfFrameAsync();

            // Resume game before fade in
            if (pausedByTransition)
            {
                PauseController.SetPause(false);
                pausedByTransition = false;
            }

            // Fade in
            if (ScreenFader.Instance != null)
            {
                await ScreenFader.Instance.FadeIn();
            }

            // Restore confiner damping
            if (confiner != null)
            {
                confiner.Damping = savedConfinerDamping;
            }

            return true;
        }
        finally
        {
            if (pausedByTransition)
            {
                PauseController.SetPause(false);
            }

            isTransitioning = false;
        }
    }
}
