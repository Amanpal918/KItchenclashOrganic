using UnityEngine;

public class GroceryBagClickHandler : MonoBehaviour
{
    private HootCutsceneManager manager;

    void Start()
    {
        // Find the main manager component in the kitchen scene
        manager = FindFirstObjectByType<HootCutsceneManager>();
    }

    // 🌟 Unity automatically runs this whenever this object's collider is clicked!
    void OnMouseDown()
    {
        if (manager != null && manager.isBagClickable)
        {
            Debug.Log("🛍️ [Input System Override] Direct Box Collider touch detected on the grocery bag asset!");
            manager.OnBagClicked();
        }
    }
}