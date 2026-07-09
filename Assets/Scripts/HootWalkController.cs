using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class HootCutsceneManager : MonoBehaviour
{
    // 🌟 System data design configuration structure
    [System.Serializable]
    public struct UnlockableRewardItem
    {
        public string rewardTitleText;
        [TextArea(2, 4)] public string rewardDescriptionText;
        public Sprite rewardIconSprite;
        public GameObject sceneGameplayItemToEnable; // The actual object hidden in the kitchen
    }

    [Header("🦉 Cutscene Target References")]
    [SerializeField] private GameObject hootCharacterObject;
    [SerializeField] private GameObject mainOpenDoorObject;
    [SerializeField] private string starSaveKey = "Lumi_TotalStars";
    private string cutscenePlayedSaveKey = "Hoot_Cutscene_Played";

    [Header("🛍️ Grocery Bag Configurations")]
    [SerializeField] private GameObject standaloneGroceryBagObject;
    [HideInInspector] public bool isBagClickable = false;

    [Header("🗂️ Multi-Page Dynamic Reward Panel List")]
    [SerializeField] private GameObject rewardUiPanelParent; // The main parent Reward_Panel
    [SerializeField] private TextMeshProUGUI uiTitleText;     // Drag Title_Text here
    [SerializeField] private TextMeshProUGUI uiDescriptionText; // Drag Description_Text here
    [SerializeField] private Image uiIconImage;              // Drag Unlocked_Item_Icon here
    [SerializeField] private Button panelOverlayInvisibleButton; // Drag an invisible button component here

    [Space(10)]
    [SerializeField] private List<UnlockableRewardItem> rewardItemsSequenceList = new List<UnlockableRewardItem>();

    [Header("📐 Target Destination Settings")]
    [SerializeField] private float destinationXPosition = 20f;
    [SerializeField] private float travelDuration = 10f;

    private Animator hootAnimator;
    private bool cutsceneTriggered = false;
    private int currentRewardPageIndex = 0; // Tracks what item screen page we are currently looking at

    void Start()
    {
        bool alreadyPlayed = PlayerPrefs.GetInt(cutscenePlayedSaveKey, 0) == 1;

        if (alreadyPlayed)
        {
            cutsceneTriggered = true;
            SetAllObjectsToEndGameplayState();
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
            if (currentStars >= 25)
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
        if (rewardUiPanelParent != null) rewardUiPanelParent.SetActive(false);

        // 🌟 SAFE LOOP: Prevents editor locking up
        if (rewardItemsSequenceList != null)
        {
            for (int i = 0; i < rewardItemsSequenceList.Count; i++)
            {
                if (rewardItemsSequenceList[i].sceneGameplayItemToEnable != null)
                    rewardItemsSequenceList[i].sceneGameplayItemToEnable.SetActive(false);
            }
        }
    }

    private void SetAllObjectsToEndGameplayState()
    {
        if (mainOpenDoorObject != null) mainOpenDoorObject.SetActive(true);
        if (hootCharacterObject != null) hootCharacterObject.SetActive(true);
        if (standaloneGroceryBagObject != null) standaloneGroceryBagObject.SetActive(false);
        if (rewardUiPanelParent != null) rewardUiPanelParent.SetActive(false);

        // 🌟 SAFE LOOP
        if (rewardItemsSequenceList != null)
        {
            for (int i = 0; i < rewardItemsSequenceList.Count; i++)
            {
                if (rewardItemsSequenceList[i].sceneGameplayItemToEnable != null)
                    rewardItemsSequenceList[i].sceneGameplayItemToEnable.SetActive(true);
            }
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

            hootCharacterObject.transform.DOMoveX(destinationXPosition, travelDuration)
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
            Debug.Log("🎯 Hoot has stopped walking! Standalone grocery bag is now CLICKABLE.");
        }
    }

    public void OnBagClicked()
    {
        if (!isBagClickable) return;
        isBagClickable = false;

        if (standaloneGroceryBagObject != null) standaloneGroceryBagObject.SetActive(false);

        // Open panel parent and render the very first item page data profile
        if (rewardUiPanelParent != null && rewardItemsSequenceList.Count > 0)
        {
            rewardUiPanelParent.SetActive(true);
            currentRewardPageIndex = 0;
            RenderCurrentRewardPageData();

            // Wire up the click callback button dynamically
            if (panelOverlayInvisibleButton != null)
            {
                panelOverlayInvisibleButton.onClick.RemoveAllListeners();
                panelOverlayInvisibleButton.onClick.AddListener(HandlePanelClickTraversal);
            }
        }
    }

    private void RenderCurrentRewardPageData()
    {
        if (currentRewardPageIndex >= rewardItemsSequenceList.Count) return;

        UnlockableRewardItem currentData = rewardItemsSequenceList[currentRewardPageIndex];

        if (uiTitleText != null) uiTitleText.text = currentData.rewardTitleText;
        if (uiDescriptionText != null) uiDescriptionText.text = currentData.rewardDescriptionText;
        if (uiIconImage != null) uiIconImage.sprite = currentData.rewardIconSprite;

        Debug.Log($"📑 [Panel Page Swapped] Now showing screen page index: {currentRewardPageIndex} ({currentData.rewardTitleText})");
    }

    private void HandlePanelClickTraversal()
    {
        // 1. Instantly activate the real physical gameplay cooking entity in the scene
        UnlockableRewardItem completedItem = rewardItemsSequenceList[currentRewardPageIndex];
        if (completedItem.sceneGameplayItemToEnable != null)
        {
            completedItem.sceneGameplayItemToEnable.SetActive(true);
            Debug.Log($"⚡ [Item Unlocked] Activated scene cooking tool element: {completedItem.sceneGameplayItemToEnable.name}");
        }

        // 2. Advance pointer page counter forward
        currentRewardPageIndex++;

        // 3. Determine if we have more pages to draw or if we reached the finish line
        if (currentRewardPageIndex < rewardItemsSequenceList.Count)
        {
            RenderCurrentRewardPageData();
        }
        else
        {
            // 🎉 Closed system loop clean termination
            if (rewardUiPanelParent != null) rewardUiPanelParent.SetActive(false);
            Debug.Log("🏁 All rewards claimed! Turning overlay off and starting core kitchen gameplay loop.");
        }
    }
}