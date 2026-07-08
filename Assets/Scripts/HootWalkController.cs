using UnityEngine;
using DG.Tweening;

public class HootCutsceneManager : MonoBehaviour
{
    [Header("🦉 Cutscene Targets")]
    [SerializeField] private GameObject hootCharacterObject;
    [SerializeField] private GameObject mainOpenDoorObject;
    [SerializeField] private string starSaveKey = "Lumi_TotalStars";

    // 🌟 PERMANENT SAVE KEY: Keeps track of whether the cutscene has ever been played
    private string cutscenePlayedSaveKey = "Hoot_Cutscene_Played";

    [Header("📐 Target Destination Settings")]
    [SerializeField] private float destinationXPosition = 5f;
    [SerializeField] private float travelDuration = 10f;

    private Animator hootAnimator;
    private bool cutsceneTriggered = false;

    void Start()
    {
        // Check if the player already completed this milestone in a past play session
        bool alreadyPlayed = PlayerPrefs.GetInt(cutscenePlayedSaveKey, 0) == 1;

        if (alreadyPlayed)
        {
            cutsceneTriggered = true; // Lock the script tracking permanently

            // 🌟 POST-CUTSCENE ENVIRONMENT SETUP:
            // The door stays open, but Hoot disappears cleanly from the room!
            if (mainOpenDoorObject != null) mainOpenDoorObject.SetActive(true);
            if (hootCharacterObject != null) hootCharacterObject.SetActive(false);
        }
        else
        {
            // If they haven't reached 25 stars yet, keep everything hidden safely
            if (hootCharacterObject != null) hootCharacterObject.SetActive(false);
            if (mainOpenDoorObject != null) mainOpenDoorObject.SetActive(false);
        }
    }

    void Update()
    {
        // If it already ran once, jump out immediately. It will NEVER run again on 26+ stars!
        if (cutsceneTriggered) return;

        int currentStars = PlayerPrefs.GetInt(starSaveKey, 0);

        // 🌟 STRICT MATCH: Only fires right when they hit or cross the 25-star milestone frame!
        if (currentStars >= 25)
        {
            TriggerArrivalCutscene();
        }
    }

    private void TriggerArrivalCutscene()
    {
        cutsceneTriggered = true;

        // 💾 SAVE COMPLETED STATUS TO DISK: Lock it so it never resets when scoring points
        PlayerPrefs.SetInt(cutscenePlayedSaveKey, 1);
        PlayerPrefs.Save();

        // 1. Swing open the entry point door
        if (mainOpenDoorObject != null)
        {
            mainOpenDoorObject.SetActive(true);
        }

        // 2. Wake up Hoot
        if (hootCharacterObject != null)
        {
            hootCharacterObject.SetActive(true);
            hootAnimator = hootCharacterObject.GetComponent<Animator>();
        }

        // 3. Smooth Tween walk movement
        if (hootCharacterObject != null)
        {
            hootCharacterObject.transform.DOMoveX(destinationXPosition, travelDuration)
                 .SetEase(Ease.Linear)
                 .OnComplete(TransitionToRestingState);
        }
    }

    private void TransitionToRestingState()
    {
        if (hootAnimator != null)
        {
            hootAnimator.SetTrigger("StopWalking");
        }

        Debug.Log("🎯 Cutscene complete! Hoot arrived at her spot.");
        if (hootCharacterObject != null)
        {
            hootCharacterObject.SetActive(false);
        }
        if (mainOpenDoorObject != null)
        {
            mainOpenDoorObject.SetActive(false);
        }
        // 🌟 OPTIONAL DISAPPEAR HOOK: If you want Hoot to vanish the exact split-second 
        // she finishes walking instead of remaining visible on screen, uncomment the line below:
        // hootCharacterObject.SetActive(false);
    }
}