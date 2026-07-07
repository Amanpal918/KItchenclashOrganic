using UnityEngine;

public class MilkPourer : MonoBehaviour
{
    [Header("🥛 Visual & Liquid Components")]
    [SerializeField] private ParticleSystem pourStreamParticles;

    [Header("📐 Magnet Positioning Settings")]
    [SerializeField] private float activationDistance = 2.5f;
    [SerializeField] private float nozzleHeightOffset = 1.2f;
    [SerializeField] private float pourTiltAngleZ = -90f;

    [Header("⏰ Countdown Registration Settings")]
    [SerializeField] private float targetedPourDuration = 3f;

    private bool isNearTarget = false;
    private Collider2D activeTargetCollider;
    private Transform parentRoot;

    // Running State Clocks
    private float progressivePourTimer = 0f;
    private bool calculationCompleted = false;

    void Start()
    {
        parentRoot = transform.parent;
        if (pourStreamParticles != null) pourStreamParticles.Stop();
    }

    public void UpdatePourInteraction(Collider2D targetCollider)
    {
        activeTargetCollider = targetCollider;
        isNearTarget = true;

        float targetRimY = targetCollider.bounds.max.y;
        Transform trackTarget = parentRoot != null ? parentRoot : transform;

        float currentX = trackTarget.position.x;
        float currentY = trackTarget.position.y;
        float distanceX = Mathf.Abs(currentX - targetCollider.bounds.center.x);

        if (distanceX <= activationDistance && currentY >= targetRimY - 2.5f)
        {
            float magnetFactor = Mathf.InverseLerp(activationDistance, 0.4f, distanceX);
            magnetFactor = Mathf.Clamp01(magnetFactor);

            float finalAngle = Mathf.Lerp(0f, pourTiltAngleZ, magnetFactor);
            transform.localRotation = Quaternion.Euler(0f, 0f, finalAngle);

            float lockedHeightY = targetRimY + nozzleHeightOffset;
            float finalY = Mathf.Lerp(currentY, lockedHeightY, magnetFactor);

            if (parentRoot != null)
            {
                parentRoot.position = new Vector3(currentX, finalY, parentRoot.position.z);
            }
            else
            {
                transform.position = new Vector3(currentX, finalY, transform.position.z);
            }

            // ── Active Streaming Handler ──
            if (magnetFactor >= 0.8f)
            {
                // 🌟 FIX: Only allow the stream to play if the calculation hasn't finished yet!
                if (pourStreamParticles != null && !pourStreamParticles.isPlaying && !calculationCompleted)
                {
                    pourStreamParticles.Play();
                }

                // ── Countdown Timing Simulation ──
                if (!calculationCompleted)
                {
                    progressivePourTimer += Time.deltaTime;

                    if (progressivePourTimer >= targetedPourDuration)
                    {
                        calculationCompleted = true;

                        // 🌟 PRINT TO CONSOLE
                        Debug.Log("Milk is added to blender");

                        // Stop the particle stream instantly
                        StopPourStream();

                        // Register contents
                        if (targetCollider.TryGetComponent<BlenderController>(out var blender))
                        {
                            blender.AddLiquidIngredientDirect("Milk");
                        }
                    }
                }
            }
            else
            {
                StopPourStream();
            }
        }
        else
        {
            ResetPourState();
        }
    }

    public void ResetPourState()
    {
        StopPourStream();
        transform.localRotation = Quaternion.identity;
        isNearTarget = false;
        progressivePourTimer = 0f; // Safely wipe progress if pulled away early
    }

    public void HandleReleaseOverBlender()
    {
        if (!isNearTarget) return;

        ResetPourState();

        if (activeTargetCollider != null)
        {
            if (activeTargetCollider.GetComponent<BlenderController>() != null)
            {
                IngredientController currentItem = parentRoot != null ? parentRoot.GetComponent<IngredientController>() : GetComponent<IngredientController>();
                if (currentItem != null)
                {
                    activeTargetCollider.GetComponent<BlenderController>().SnapAndAddIngredient(currentItem);
                }
            }
        }

        activeTargetCollider = null;
        calculationCompleted = false;
    }

    private void StopPourStream()
    {
        if (pourStreamParticles != null && pourStreamParticles.isPlaying)
        {
            pourStreamParticles.Stop();
        }
    }
}