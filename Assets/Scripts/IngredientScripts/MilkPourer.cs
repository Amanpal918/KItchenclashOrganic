using UnityEngine;

public class MilkPourer : MonoBehaviour
{
    [Header("🥛 Visual & Liquid Components")]
    [SerializeField] private ParticleSystem pourStreamParticles;

    [Header("📐 Magnet Positioning Settings")]
    [SerializeField] private float activationDistance = 2.5f;
    [SerializeField] private float nozzleHeightOffset = 1.2f;
    [SerializeField] private float pourTiltAngleZ = -90f;

    private bool isNearTarget = false;
    private Collider2D activeTargetCollider;
    private Transform parentRoot;

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

        // If within range of the blender/glass mouth
        if (distanceX <= activationDistance && currentY >= targetRimY - 2.5f)
        {
            float magnetFactor = Mathf.InverseLerp(activationDistance, 0.4f, distanceX);
            magnetFactor = Mathf.Clamp01(magnetFactor);

            // 1. TILT CHILD ARTWORK ONLY
            float finalAngle = Mathf.Lerp(0f, pourTiltAngleZ, magnetFactor);
            transform.localRotation = Quaternion.Euler(0f, 0f, finalAngle);

            // 2. SNAP THE PARENT ROOT TO THE RIM HEIGHT
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

            // 3. TRIGGER STREAM WHEN 90 DEGREE TILT COMPLETED
            if (magnetFactor >= 0.8f)
            {
                if (pourStreamParticles != null && !pourStreamParticles.isPlaying)
                {
                    pourStreamParticles.Play();
                }
            }
            else
            {
                StopPourStream();
            }
        }
        else
        {
            // Exit condition if inside overlap block but out of magnet ranges
            ResetPourState();
        }
    }

    /// <summary>
    /// CRITICAL FIX: Forces the container artwork back upright and completely shuts down the particle emission loop.
    /// </summary>
    public void ResetPourState()
    {
        StopPourStream();
        transform.localRotation = Quaternion.identity;
        isNearTarget = false;
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
    }

    private void StopPourStream()
    {
        if (pourStreamParticles != null && pourStreamParticles.isPlaying)
        {
            pourStreamParticles.Stop();
        }
    }
}