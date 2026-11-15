using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TimeShiftPulseTestRunner
{
    public static void VerifyPulseEffect()
    {
        Debug.Log("[TEST RUNNER] Starting TimeShiftPulseEffect verification...");

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MovementTest.unity");
        if (!scene.IsValid())
        {
            Debug.LogError("[TEST FAILED] Could not open Assets/Scenes/MovementTest.unity");
            EditorApplication.Exit(1);
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }

        if (player == null)
        {
            Debug.LogError("[TEST FAILED] Player GameObject not found in MovementTest scene.");
            EditorApplication.Exit(1);
            return;
        }

        var controller = player.GetComponent<TimeShiftController>();
        if (controller == null)
        {
            Debug.LogError("[TEST FAILED] TimeShiftController missing on Player.");
            EditorApplication.Exit(1);
            return;
        }

        var pulseEffect = player.GetComponent<TimeShiftPulseEffect>();
        if (pulseEffect == null)
        {
            Debug.LogError("[TEST FAILED] TimeShiftPulseEffect missing on Player.");
            EditorApplication.Exit(1);
            return;
        }

        var collider = player.GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("[TEST FAILED] Collider2D missing on Player.");
            EditorApplication.Exit(1);
            return;
        }

        // Verify pulse origin matches collider center
        Vector2 expectedCenter = (Vector2)collider.bounds.center + pulseEffect.CenterOffset;
        Vector2 actualCenter = pulseEffect.PulseCenter;
        if (Vector2.Distance(expectedCenter, actualCenter) > 0.001f)
        {
            Debug.LogError($"[TEST FAILED] PulseCenter {actualCenter} did not match expected {expectedCenter}");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[TEST PASS] PulseCenter verified at {actualCenter} (Player Collider Center).");

        // Verify controller reference to pulseEffect
        if (controller.PulseEffect != pulseEffect)
        {
            Debug.LogError("[TEST FAILED] controller.PulseEffect does not reference TimeShiftPulseEffect component.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[TEST PASS] TimeShiftController.PulseEffect reference successfully verified.");

        // Verify configuration properties
        if (pulseEffect.PulseDuration <= 0f)
        {
            Debug.LogError("[TEST FAILED] pulseDuration must be greater than 0.");
            EditorApplication.Exit(1);
            return;
        }

        if (pulseEffect.TargetRadius <= pulseEffect.StartRadius)
        {
            Debug.LogError("[TEST FAILED] targetRadius must be greater than startRadius.");
            EditorApplication.Exit(1);
            return;
        }

        if (pulseEffect.RingCount < 1)
        {
            Debug.LogError("[TEST FAILED] ringCount must be at least 1.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[TEST PASS] Pulse effect parameters: Duration={pulseEffect.PulseDuration}s, Radius=[{pulseEffect.StartRadius} -> {pulseEffect.TargetRadius}], RingCount={pulseEffect.RingCount}.");

        Debug.Log("[TEST COMPLETED] All TimeShiftPulseEffect verifications passed successfully!");
        EditorApplication.Exit(0);
    }
}
