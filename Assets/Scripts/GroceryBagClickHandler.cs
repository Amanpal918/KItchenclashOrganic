using UnityEngine;

public class GroceryBagClickHandler : MonoBehaviour
{
    private HootCutsceneManager manager;

    void Start()
    {
        // Find the main manager component in the kitchen scene
        manager = FindFirstObjectByType<HootCutsceneManager>(); //
    }

    // 🌟 This works independently of the Fridge script's raycasting loop blocks!
    void OnMouseDown() //[cite: 1]
    {
        if (manager == null) return;

        // Force a direct check on the manager state layout
        if (manager.isBagClickable) //[cite: 1]
        {
            Debug.Log("🛍️ [Bag Click] Click passed directly to manager successfully!");
            manager.OnBagClicked(); //[cite: 1]
        }
        else
        {
            Debug.LogWarning("⚠️ [Bag Click] Click intercepted, but 'isBagClickable' is currently FALSE on HootCutsceneManager!");
        }
    }
}