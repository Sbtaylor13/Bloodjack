using UnityEngine;

/// <summary>
/// The scalpel item. When held, allows the player to collect money cubes.
/// Attach to the scalpel GameObject in the room scene.
/// </summary>
public class ScalpelItem : Item
{
    void Awake()
    {
        itemName    = "Scalpel";
        pickupPrompt = "Pick up";
        destroyOnPickup = false; // scalpel stays in inventory, its world GO is hidden
    }

    public override void OnPickup(PlayerInventory inventory)
    {
        // Hide the world object but don't destroy — keep it for reference
        gameObject.SetActive(false);
        inHand?.SetActive(true);
        Debug.Log("[ScalpelItem] Scalpel picked up.");
    }

    public override void OnUnequip(PlayerInventory inventory)
    {
        Debug.Log("[ScalpelItem] Scalpel unequipped.");
    }
}
