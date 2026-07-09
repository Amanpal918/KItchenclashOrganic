using System.Collections;
using UnityEngine;
using TMPro;

public class CharacterPersonality : MonoBehaviour
{
    // Define the appreciation tiers cleanly
    public enum FoodReactionTier { LovesIt, LikesIt, NotSure, DislikesIt }

    [Header("⭐ PlayerPrefs Saving Keys")]
    [SerializeField] private string characterStarSaveKey = "Lumi_TotalStars";

    [Header("🎬 Animator Parameter Names")]
    [SerializeField] private string loveTriggerName = "LumiLove";
    [SerializeField] private string likeTriggerName = "LumiLike";
    [SerializeField] private string thinkTriggerName = "LumiThink";
    [SerializeField] private string dislikeTriggerName = "LumiDislike";
    [SerializeField] private string fallbackIdleBoolName = "isFlying";

    [Header("📺 UI Display Configuration")]
    [Tooltip("Drag your Screen Star TextMeshPro component here via the Inspector!")]
    [SerializeField] private TextMeshProUGUI globalStarCounterText;

    [Header("⭐ Dynamic Star Popup Settings")]
    [SerializeField] private GameObject starUiPrefab;       // Drag your UI_Star_Prefab here
    [SerializeField] private Transform starPopupContainer; // Drag your Star_Popup_Container here
    [SerializeField] private float popupDisplayDuration = 2.0f;

    // Internal state cache variables
    private Animator myAnimator;
    private int lifetimeStars = 0;

    void Awake()
    {
        myAnimator = GetComponent<Animator>();

        // 💾 LOAD ON STARTUP: Pull absolute lifetime star count from permanent local disk storage
        lifetimeStars = PlayerPrefs.GetInt(characterStarSaveKey, 0);
        Debug.Log($"Lumi initialized! Absolute Lifetime Stars loaded: {lifetimeStars}");

        // 🌟 Initial UI setup so the player sees their correct saved stars immediately when the scene opens
        UpdateStarDisplayUI();
    }

    /// <summary>
    /// 🚀 NEW EXPLICIT FEATURE ENTRY POINT: Call this method when dropping food onto Lumi's mouth!
    /// Handles calculations AND recycles the object back into the optimization pool safely.
    /// </summary>
    public void FeedLumiFoodObject(GameObject foodObject)
    {
        if (foodObject == null) return;

        Debug.Log($"🍽️ [Lumi Intake] Processing ingestion for item asset: '{foodObject.name}'");

        // 1. Process all existing score tracking formulas and spawn animations using the object's name
        ProcessFedFoodItem(foodObject.name);

        // 2. Performance optimization: Recycle the physical item back to pool storage instantly instead of running Destroy()
        if (TrashAndPoolManager.Instance != null)
        {
            TrashAndPoolManager.Instance.RecycleToPool(foodObject);
            Debug.Log($"✅ [Pool Recycle Intercept] Sent '{foodObject.name}' to object pool cache smoothly.");
        }
        else
        {
            // Fallback safety guard if the Pool Manager isn't active or loaded in the scene
            Debug.LogWarning("⚠️ [Pool Warning] TrashAndPoolManager.Instance is missing from the scene! Falling back to raw destroy.");
            Destroy(foodObject);
        }
    }

    /// <summary>
    /// Processes a recipe string identity token and calculates animations, rewards, and saves states.
    /// </summary>
    public void ProcessFedFoodItem(string foodItemName)
    {
        FoodReactionTier reaction = DetermineReactionTier(foodItemName);
        int starsEarned = 0;

        switch (reaction)
        {
            case FoodReactionTier.LovesIt:
                starsEarned = 3;
                PlayReactionAnimation(loveTriggerName);
                Debug.Log($"🥰 Lumi LOVED the {foodItemName}! Fired Trigger: {loveTriggerName}");
                break;

            case FoodReactionTier.LikesIt:
                starsEarned = 2;
                PlayReactionAnimation(likeTriggerName);
                Debug.Log($"😊 Lumi LIKED the {foodItemName}! Fired Trigger: {likeTriggerName}");
                break;

            case FoodReactionTier.NotSure:
                starsEarned = 1;
                PlayReactionAnimation(thinkTriggerName);
                Debug.Log($"🤔 Lumi was NOT SURE about the {foodItemName}. Fired Trigger: {thinkTriggerName}");
                break;

            case FoodReactionTier.DislikesIt:
                starsEarned = 0;
                PlayReactionAnimation(dislikeTriggerName);
                Debug.Log($"🤢 Lumi DISLIKED the {foodItemName}!");
                break;
        }

        if (starsEarned > 0)
        {
            // 🌟 Keep your math functions synchronized!
            AddAndSaveStars(starsEarned);
            StartCoroutine(SpawnStarPopupRoutine(starsEarned));
        }
    }

    private IEnumerator SpawnStarPopupRoutine(int count)
    {
        // 1. Clear out any old stars just in case
        foreach (Transform child in starPopupContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Spawn the exact amount side-by-side inside the layout group container
        for (int i = 0; i < count; i++)
        {
            GameObject spawnedStar = Instantiate(starUiPrefab, starPopupContainer);

            // 🌟 FORCE ANIMATION TO PLAY ON SPAWN
            Animator starAnim = spawnedStar.GetComponent<Animator>();
            if (starAnim != null)
            {
                starAnim.Play("Star_Animation", 0, 0f);
            }
        }

        // 3. Keep them alive on screen for a moment for the player to see
        yield return new WaitForSeconds(popupDisplayDuration);

        // 4. Clean up and destroy them smoothly
        foreach (Transform child in starPopupContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private FoodReactionTier DetermineReactionTier(string foodName)
    {
        // Clean up text extensions to support both raw items and cloned instances safely
        string itemKey = foodName.ToLower().Replace("(clone)", "").Trim();

        // 1. LOVES IT CHECK
        if (itemKey == "rainbow smoothie" || itemKey == "strawberry shake" || itemKey.Contains("shake") || itemKey.Contains("smoothie"))
        {
            return FoodReactionTier.LovesIt;
        }

        // 2. DISLIKES IT CHECK
        if (itemKey == "burnt food" || itemKey.Contains("burnt"))
        {
            return FoodReactionTier.DislikesIt;
        }

        // 3. LIKES IT CHECK (Fruits, generic health foods)
        if (itemKey == "apple" || itemKey == "banana" || itemKey == "kiwi" ||
            itemKey == "orange" || itemKey == "strawberry" || itemKey == "blueberry" ||
            itemKey == "grapes" || itemKey == "mango")
        {
            return FoodReactionTier.LikesIt;
        }

        // 4. FALLBACK: Default neutral reaction tier
        return FoodReactionTier.NotSure;
    }

    private void PlayReactionAnimation(string targetTrigger)
    {
        if (myAnimator == null) return;

        myAnimator.SetBool(fallbackIdleBoolName, false);
        myAnimator.SetTrigger(targetTrigger);
    }

    private void AddAndSaveStars(int amount)
    {
        lifetimeStars += amount;

        // 💾 SAVE TO DISK INSTANTLY
        PlayerPrefs.SetInt(characterStarSaveKey, lifetimeStars);
        PlayerPrefs.Save();

        Debug.Log($"⭐ Stars Added: +{amount} | Absolute New Total Balance: {lifetimeStars}");

        // 🌟 Refresh the display text mesh values immediately!
        UpdateStarDisplayUI();
    }

    private void UpdateStarDisplayUI()
    {
        if (globalStarCounterText != null)
        {
            globalStarCounterText.text = "⭐ " + lifetimeStars.ToString();
        }
    }
}