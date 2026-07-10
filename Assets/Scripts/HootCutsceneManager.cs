using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class HootCutsceneManager : MonoBehaviour
{
    [Header("🦉 Cutscene Target References")]
    [SerializeField] private GameObject hootCharacterObject;
    [SerializeField] private GameObject mainOpenDoorObject;
    [SerializeField] private string starSaveKey = "Lumi_TotalStars";
    private string cutscenePlayedSaveKey = "Hoot_Cutscene_Played";

    [Header("🛍️ Table Delivery Setup")]
    [SerializeField] private GameObject standaloneGroceryBagObject;
    [HideInInspector] public bool isBagClickable = false;

    [Header("📦 Physical Reward Items")]
    [SerializeField] private GameObject toasterObject;
    [SerializeField] private GameObject breadObject;
    [SerializeField] private GameObject butterObject;

    [Header("📐 Target Destination Settings")]
    [SerializeField] private float destinationXPosition = 9.3f;
    [SerializeField] private float destinationYPosition = -4.2f;
    [SerializeField] private float travelDuration = 10f;

    private Animator hootAnimator;
    private bool cutsceneTriggered = false;
    private int popOutSequenceStage = 0;

    void Start()
    {
        bool alreadyPlayed = PlayerPrefs.GetInt(cutscenePlayedSaveKey, 0) == 1;

        if (alreadyPlayed)
        {
            cutsceneTriggered = true;
            SetAllObjectsToFinalGameplayState();
        }
        else
        {
            InitializeHiddenStartupState();
        }
    }

    void Update()
    {
        if (!cutsceneTriggered)
        {
            int currentStars = PlayerPrefs.GetInt(starSaveKey, 0);
            if (currentStars >= 2)
            {
                TriggerArrivalCutscene();
            }
        }
    }

    private void InitializeHiddenStartupState()
    {
        if (hootCharacterObject != null) hootCharacterObject.SetActive(false);
        if (mainOpenDoorObject != null) mainOpenDoorObject.SetActive(false);
        if (standaloneGroceryBagObject != null) standaloneGroceryBagObject.SetActive(false);

        if (toasterObject != null) toasterObject.SetActive(false);
        if (breadObject != null) breadObject.SetActive(false);
        if (butterObject != null) butterObject.SetActive(false);
    }

    private void SetAllObjectsToFinalGameplayState()
    {
        if (mainOpenDoorObject != null) mainOpenDoorObject.SetActive(true);
        if (hootCharacterObject != null) hootCharacterObject.SetActive(true);
        if (standaloneGroceryBagObject != null) standaloneGroceryBagObject.SetActive(false);

        if (toasterObject != null) toasterObject.SetActive(true);
        if (breadObject != null) breadObject.SetActive(true);
        if (butterObject != null) butterObject.SetActive(true);
    }

    private void TriggerArrivalCutscene()
    {
        cutsceneTriggered = true;
        PlayerPrefs.SetInt(cutscenePlayedSaveKey, 1);
        PlayerPrefs.Save();

        if (mainOpenDoorObject != null) mainOpenDoorObject.SetActive(true);

        if (hootCharacterObject != null)
        {
            hootCharacterObject.SetActive(true);
            hootAnimator = hootCharacterObject.GetComponent<Animator>();

            // 🌟 OLD LAYER SETUP RESTORED: Hardcoding Hoot back to Default layer
            SpriteRenderer hootRenderer = hootCharacterObject.GetComponent<SpriteRenderer>();
            if (hootRenderer != null)
            {
                hootRenderer.sortingLayerName = "Characters";
                hootRenderer.sortingOrder = 2;
            }

            Vector3 targetWalkingDestination = new Vector3(destinationXPosition, destinationYPosition, 0f);

            hootCharacterObject.transform.DOMove(targetWalkingDestination, travelDuration)
                 .SetEase(Ease.Linear)
                 .OnComplete(TransitionToRestingState);
        }
    }

    private void TransitionToRestingState()
    {
        if (hootAnimator != null) hootAnimator.SetTrigger("StopWalking");

        if (standaloneGroceryBagObject != null)
        {
            standaloneGroceryBagObject.SetActive(true);
            isBagClickable = true;
            Debug.Log("🎯 Hoot reached the table! Standalone grocery bag is now CLICKABLE.");
        }
    }

    public void OnBagClicked()
    {
        if (!isBagClickable) return;

        Vector3 bagWorldPos = standaloneGroceryBagObject.transform.position;

        switch (popOutSequenceStage)
        {
            case 0:
                PopItemOntoTable(toasterObject, bagWorldPos, new Vector3(12.2f, -4.3f, 0f));
                popOutSequenceStage++;
                break;

            case 1:
                PopItemOntoTable(breadObject, bagWorldPos, new Vector3(13.6f, -4.3f, 0f));
                popOutSequenceStage++;
                break;

            case 2:
                PopItemOntoTable(butterObject, bagWorldPos, new Vector3(15.0f, -4.3f, 0f));

                isBagClickable = false;
                standaloneGroceryBagObject.SetActive(false);
                Debug.Log("🏁 All items delivered onto the table surface layout.");
                break;
        }
    }

    private void PopItemOntoTable(GameObject targetObj, Vector3 spawnOrigin, Vector3 tableLandingSpot)
    {
        if (targetObj == null) return;

        FlyToHomeItem flyScript = targetObj.GetComponent<FlyToHomeItem>();
        if (flyScript != null)
        {
            flyScript.PrepareForTableDisplay();
        }

        targetObj.transform.position = spawnOrigin;
        targetObj.SetActive(true);

        targetObj.transform.DOJump(tableLandingSpot, 1.8f, 1, 0.6f)
            .SetEase(Ease.OutQuad);
    }
}