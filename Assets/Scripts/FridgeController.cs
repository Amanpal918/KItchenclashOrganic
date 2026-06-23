using UnityEngine;
using UnityEngine.UI;

public class FridgeController : MonoBehaviour
{
    public Image fridgeImage;
    public Sprite openFridgeSprite;
    public GameObject fridgeInventoryUI;

    private bool isOpen = false;

    void Start()
    {
        fridgeImage.enabled = false;
        fridgeInventoryUI.SetActive(false);
        Debug.Log("Fridge Start called");
    }

    public void ToggleFridge()
    {
        isOpen = !isOpen;
        fridgeImage.enabled = isOpen;
        fridgeInventoryUI.SetActive(isOpen);
        Debug.Log("Toggle called, isOpen = " + isOpen);
    }
}