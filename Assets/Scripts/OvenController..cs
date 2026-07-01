using UnityEngine;

public class OvenController : MonoBehaviour
{
    [Header("🖼️ Sprite States")]
    [SerializeField] private Sprite closedOvenSprite;
    [SerializeField] private Sprite openOvenSprite;

    private SpriteRenderer spriteRenderer;
    private bool isOpen = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Ensure we start with the closed sprite if assigned
        if (closedOvenSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = closedOvenSprite;
        }
    }

    // This triggers automatically when the player taps/clicks the oven's collider
    void OnMouseDown()
    {
        // Toggle the state flag
        isOpen = !isOpen;

        // Swap the texture art dynamically
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = isOpen ? openOvenSprite : closedOvenSprite;
            Debug.Log("Oven State Toggled! Is Open: " + isOpen);
        }
    }
}