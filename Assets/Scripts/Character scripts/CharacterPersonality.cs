using System.Collections;
using UnityEngine;

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

    // Internal state cache variables
    private Animator myAnimator;
    private int lifetimeStars = 0;

    void Awake()
    {
        myAnimator = GetComponent<Animator>();

        // 💾 LOAD ON STARTUP: Pull absolute lifetime star count from permanent local disk storage
        lifetimeStars = PlayerPrefs.GetInt(characterStarSaveKey, 0);
        Debug.Log($"Lumi initialized! Absolute Lifetime Stars loaded: {lifetimeStars}");
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
                // Add a unique dislike state trigger later if your team makes one!
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
        // Force strings to lowercase to ensure spelling mistakes don't break lookups
        string itemKey = foodName.ToLower().Trim();

        // 1. LOVES IT CHECK
        if (itemKey == "rainbow smoothie" || itemKey == "strawberry shake")
        {
            return FoodReactionTier.LovesIt;
        }

        // 2. DISLIKES IT CHECK
        if (itemKey == "burnt food" || itemKey.Contains("burnt"))
        {
            return FoodReactionTier.DislikesIt;
        }

        // 3. LIKES IT CHECK (Fruits, Smoothies, generic health foods)
        if (itemKey == "apple" || itemKey == "banana" || itemKey == "kiwi" ||
            itemKey == "orange" || itemKey == "strawberry" || itemKey == "blueberry" ||
            itemKey == "grapes" || itemKey == "mango" || itemKey.Contains("smoothie"))
        {
            return FoodReactionTier.LikesIt;
        }

        // 4. FALLBACK: Default neutral reaction tier
        return FoodReactionTier.NotSure;
    }

    private void PlayReactionAnimation(string targetTrigger)
    {
        if (myAnimator == null) return;

        // Force idle fly state parameters off so they don't block the priority overlays
        myAnimator.SetBool(fallbackIdleBoolName, false);

        // Fire the specific parameter trigger instantly
        myAnimator.SetTrigger(targetTrigger);
    }

    private void AddAndSaveStars(int amount)
    {
        lifetimeStars += amount;

        // 💾 SAVE TO DISK INSTANTLY: Commit the new integer value securely to PlayerPrefs local cache
        PlayerPrefs.SetInt(characterStarSaveKey, lifetimeStars);
        PlayerPrefs.Save(); // Force write to persistent app hardware configurations

        Debug.Log($"⭐ Stars Added: +{amount} | Absolute New Total Balance: {lifetimeStars}");
    }
}