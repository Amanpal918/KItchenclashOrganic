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

    [Header("🗂️ Fridge Hierarchy Settings")]
    [SerializeField] private Transform fridgeShelfTargetParent; // ⚠️ Drag "Shelf_top" here in the Inspector!

    // 📍 PRECISE DEFINITIVE WORKSTATION COORDINATES FROM YOUR INSPECTOR IMAGES
    private Vector3 toasterPermanentWorldPos = new Vector3(22.73f, 1.3f, 0f);
    private Vector3 breadPermanentWorldPos = new Vector3(10.12f, 1.06f, 0f);
    private Vector3 butterPermanentLocalPos = new Vector3(-0.664f, 0.02850008f, 0f);

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

        if (butterObject != null)
        {
            butterObject.transform.SetParent(null); // 🌟 Ensure it starts outside fridge hierarchy on fresh run!
            butterObject.SetActive(false);
        }
    }

    private void SetAllObjectsToFinalGameplayState()
    {
        if (mainOpenDoorObject != null) mainOpenDoorObject.SetActive(true);

        if (hootCharacterObject != null)
        {
            hootCharacterObject.SetActive(true);
            hootCharacterObject.transform.position = new Vector3(destinationXPosition, destinationYPosition, 0f);

            SpriteRenderer hootRenderer = hootCharacterObject.GetComponent<SpriteRenderer>();
            if (hootRenderer != null)
            {
                hootRenderer.sortingLayerName = "Characters";
                hootRenderer.sortingOrder = 2;
            }

            Animator hootAnim = hootCharacterObject.GetComponent<Animator>();
            if (hootAnim != null) hootAnim.SetTrigger("StopWalking");
        }

        if (standaloneGroceryBagObject != null) standaloneGroceryBagObject.SetActive(false);

        if (toasterObject != null)
        {
            toasterObject.transform.position = toasterPermanentWorldPos;
            toasterObject.SetActive(true);
            ConfigurePermanentWorkstate(toasterObject, false);
        }

        if (breadObject != null)
        {
            breadObject.transform.position = breadPermanentWorldPos;
            breadObject.SetActive(true);
            ConfigurePermanentWorkstate(breadObject, true);
        }

        if (butterObject != null)
        {
            if (fridgeShelfTargetParent != null)
            {
                butterObject.transform.SetParent(fridgeShelfTargetParent);
            }
            butterObject.transform.localPosition = butterPermanentLocalPos;
            butterObject.SetActive(true);
            ConfigurePermanentWorkstate(butterObject, true);
        }
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
        }
    }

    public void OnBagClicked()
    {
        if (!isBagClickable) return;

        Vector3 bagWorldPos = standaloneGroceryBagObject.transform.position;

        switch (popOutSequenceStage)
        {
            case 0:
                Vector3 toasterTarget = bagWorldPos + new Vector3(-1.3f, -1.0f, 0f);
                PopItemOntoRoundTable(toasterObject, bagWorldPos, toasterTarget);
                popOutSequenceStage++;
                break;

            case 1:
                Vector3 breadTarget = bagWorldPos + new Vector3(0f, -1.0f, 0f);
                PopItemOntoRoundTable(breadObject, bagWorldPos, breadTarget);
                popOutSequenceStage++;
                break;

            case 2:
                Vector3 butterTarget = bagWorldPos + new Vector3(1.3f, -1.0f, 0f);
                PopItemOntoRoundTable(butterObject, bagWorldPos, butterTarget);
                isBagClickable = false;
                standaloneGroceryBagObject.SetActive(false);
                break;
        }
    }

    private void PopItemOntoRoundTable(GameObject targetObj, Vector3 spawnOrigin, Vector3 tableLandingSpot)
    {
        if (targetObj == null) return;
        targetObj.transform.position = spawnOrigin;
        targetObj.SetActive(true);
        targetObj.transform.DOJump(tableLandingSpot, 1.5f, 1, 0.5f).SetEase(Ease.OutQuad);
    }

    public void OnItemArrivedAtPermanentHome(GameObject targetObj)
    {
        if (targetObj == toasterObject) ConfigurePermanentWorkstate(toasterObject, false);
        if (targetObj == breadObject) ConfigurePermanentWorkstate(breadObject, true);
        if (targetObj == butterObject) ConfigurePermanentWorkstate(butterObject, true);
    }

    private void ConfigurePermanentWorkstate(GameObject targetObj, bool isDispenser)
    {
        MonoBehaviour flyScript = targetObj.GetComponent("FlyToHomeItem") as MonoBehaviour;
        if (flyScript != null) flyScript.enabled = false;

        IngredientController component = targetObj.GetComponent<IngredientController>();
        if (component != null)
        {
            component.isSourceDispenser = isDispenser;
            component.enabled = true;
            component.ClearFridgeShelfStatus();
        }
    }
}