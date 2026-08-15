using UnityEngine;

/// <summary>
/// Base class for all items in the game world — scalpel, power-ups, etc.
/// Attach to any pickup object alongside a Collider.
/// </summary>
public class Item : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "Item";
    public Sprite icon;                  // optional — for future HUD display
    public bool destroyOnPickup = true;  // false = item stays in world (e.g. a switch)
    public GameObject inHand;
    [Header("Pickup")]
    [Tooltip("How close the player must be to pick this up")]
    public float pickupRadius = 2.5f;
    public string pickupPrompt = "Pick up";

    /// <summary>Called by PlayerInventory when this item is picked up.</summary>
    public virtual void OnPickup(PlayerInventory inventory)
    {
        if (destroyOnPickup)
            Destroy(gameObject);
    }

    /// <summary>
    /// Called every frame by PlayerInventory while this item is the active held item.
    /// Override in subclasses to add held behaviour (e.g. scalpel sweep).
    /// </summary>
    public virtual void OnHeldUpdate(PlayerInventory inventory) { }

    /// <summary>Called when the player drops or switches away from this item.</summary>
    public virtual void OnUnequip(PlayerInventory inventory) { }
}
