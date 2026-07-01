using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class RecipeBookController : MonoBehaviour
{
    [Header("Book Visual Components")]
    [SerializeField] private SpriteRenderer bookBackgroundSR;
    [SerializeField] private SpriteRenderer recipeContentSR;

    [Header("Click Zone Colliders")]
    [SerializeField] private Collider2D mainBookCollider;
    [SerializeField] private Collider2D leftPageCollider;
    [SerializeField] private Collider2D rightPageCollider;

    [Header("Book Sprites Artwork")]
    [SerializeField] private Sprite closedBookSprite;
    [SerializeField] private Sprite openBookFrameSprite;

    [Header("Recipe Page List")]
    [SerializeField] private List<Sprite> recipePages = new List<Sprite>();

    private bool isBookOpen = false;
    private int currentPageIndex = -1;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        CloseBook();
    }

    void Update()
    {
        if (Pointer.current.press.wasPressedThisFrame)
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(Pointer.current.position.ReadValue());

            // 🌟 BULLETPROOF SOLUTION: RaycastAll finds EVERYTHING under your finger, skipping background blocking
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(worldPos.x, worldPos.y), Vector2.zero);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null)
                {
                    // If the book is open and we hit a page zone, process it and stop checking other background layers!
                    if (isBookOpen && (hit.collider == leftPageCollider || hit.collider == rightPageCollider))
                    {
                        HandleBookClick(hit.collider);
                        return;
                    }
                    // If the book is closed and we hit the main book cover, open it
                    else if (!isBookOpen && hit.collider == mainBookCollider)
                    {
                        HandleBookClick(hit.collider);
                        return;
                    }
                }
            }
        }
    }

    void HandleBookClick(Collider2D clickedCollider)
    {
        if (!isBookOpen)
        {
            if (clickedCollider == mainBookCollider)
            {
                Debug.Log("[RecipeBook] Main cover clicked. Opening Book!");
                OpenBook();
            }
            return;
        }

        if (clickedCollider == leftPageCollider)
        {
            Debug.Log("[RecipeBook] Left Page Collider hit! Closing Book.");
            CloseBook();
        }
        else if (clickedCollider == rightPageCollider)
        {
            Debug.Log("[RecipeBook] Right Page Collider hit! Flipping Page.");
            FlipRightPage();
        }
    }

    void OpenBook()
    {
        isBookOpen = true;
        currentPageIndex = 0;

        bookBackgroundSR.sprite = openBookFrameSprite;
        recipeContentSR.gameObject.SetActive(true);

        if (mainBookCollider != null) mainBookCollider.enabled = false;
        if (leftPageCollider != null) leftPageCollider.enabled = true;
        if (rightPageCollider != null) rightPageCollider.enabled = true;

        UpdateRecipePageVisual();
    }

    void CloseBook()
    {
        isBookOpen = false;
        currentPageIndex = -1;

        bookBackgroundSR.sprite = closedBookSprite;
        recipeContentSR.gameObject.SetActive(false);

        if (mainBookCollider != null) mainBookCollider.enabled = true;
        if (leftPageCollider != null) leftPageCollider.enabled = false;
        if (rightPageCollider != null) rightPageCollider.enabled = false;
    }

    void FlipRightPage()
    {
        if (recipePages.Count == 0) return;

        currentPageIndex++;
        if (currentPageIndex >= recipePages.Count)
        {
            currentPageIndex = 0;
        }
        UpdateRecipePageVisual();
    }

    void UpdateRecipePageVisual()
    {
        if (recipePages.Count > 0 && currentPageIndex >= 0)
        {
            recipeContentSR.sprite = recipePages[currentPageIndex];
        }
    }
}