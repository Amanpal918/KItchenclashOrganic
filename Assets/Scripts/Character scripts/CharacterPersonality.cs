using System.Collections;
using UnityEngine;
using TMPro; // 🌟 Need this namespace to control TextMeshPro components!

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
    [SerializeField] private string fallbackIdleBoolName = "isFlying";

    [Header("📺 UI Display Configuration")]
    [Tooltip("Drag your Screen Star TextMeshPro component here via the Inspector!")]
    [SerializeField] private TextMeshProUGUI globalStarCounterText;

    // Internal state cache variables
    private Animator myAnimator;
    private int lifetimeStars = 0;

    void Awake()
    {
        myAnimator = GetComponent<Animator>();

        // 💾 LOAD ON STARTUP: Pull absolute lifetime star count from permanent local disk storage
        lifetimeStars = PlayerPrefs.GetInt(characterStarSaveKey, 0);
        Debug.Log($"Lumi initialized! Absolute Lifetime Stars loaded: {lifetimeStars}");

        // 🌟 Initial UI setup so the player see their correct saved stars immediately when the scene opens
        UpdateStarDisplayUI();
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
                Debug.Log($"🤢 Lumi DISLIKED the {foodItemName}!");
                break;
        }

        if (starsEarned > 0)
        {
            AddAndSaveStars(starsEarned);
        }
    }

    private FoodReactionTier DetermineReactionTier(string foodName)
    {
        string itemKey = foodName.ToLower().Trim();

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

    /// <summary>
    /// 🌟 Updates the text rendering on screen to cleanly represent the data value
    /// </summary>
    private void UpdateStarDisplayUI()
    {
        if (globalStarCounterText != null)
        {
            globalStarCounterText.text = "⭐ " + lifetimeStars.ToString();
        }
    }
}