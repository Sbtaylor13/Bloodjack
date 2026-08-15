 using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the player's inventory and currently held item.
/// Attach to the Player GameObject (or its Camera child).
/// Handles pickup prompts, item switching, and forwarding held-item updates.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("Maximum distance to pick up items (raycast range)")]
    public float pickupRange = 3f;
    [Tooltip("Layer mask for interactable items")]
    public LayerMask itemLayerMask = ~0;

    [Header("Hold Position")]
    [Tooltip("Where held items appear — drag in the camera or a hand bone transform")]
    public Transform holdPoint;
    [Tooltip("Smooth speed for item moving to hold point")]
    public float holdLerpSpeed = 12f;

    [Header("UI")]
    [Tooltip("Optional world-space or screen-space prompt GameObject")]
    public GameObject pickupPromptUI;
    public TMPro.TMP_Text pickupPromptText;

    // ── State ─────────────────────────────────────────────────────────────────
    public Item HeldItem { get; private set; }
    public List<Item> AllItems { get; private set; } = new();  // full inventory

    private Camera _cam;
    private GameObject _heldVisual;  // the spawned visual for held item

    // ── Events ────────────────────────────────────────────────────────────────
    public System.Action<Item> OnItemPickedUp;
    public System.Action<Item> OnItemDropped;

    void Awake()
    {
        _cam = GetComponentInChildren<Camera>();
        if (_cam == null) _cam = Camera.main;

        // Hide prompt immediately on start
        pickupPromptUI?.SetActive(false);
    }

    void Update()
    {
        CheckForPickup();
        UpdateHeldItem();
    }

    // ── Pickup ────────────────────────────────────────────────────────────────

    void CheckForPickup()
    {
        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, pickupRange, itemLayerMask);

        Item item = hit ? hitInfo.collider.GetComponent<Item>() : null;

        if (item != null && AllItems.Contains(item))
            item = null;

        pickupPromptUI?.SetActive(item != null);

        if (pickupPromptText != null && item != null)
            pickupPromptText.text = $"{item.pickupPrompt} {item.itemName}";

        if (item != null && Input.GetKeyDown(KeyCode.E))
            PickUp(item);
    }

    void UpdateHeldItem()
    {
        if (HeldItem == null) return;

        if (_heldVisual != null && holdPoint != null)
        {
            _heldVisual.transform.position = Vector3.Lerp(
                _heldVisual.transform.position,
                holdPoint.position,
                Time.deltaTime * holdLerpSpeed
            );
            _heldVisual.transform.rotation = Quaternion.Lerp(
                _heldVisual.transform.rotation,
                holdPoint.rotation,
                Time.deltaTime * holdLerpSpeed
            );
        }

        HeldItem.OnHeldUpdate(this);

        if (Input.GetKeyDown(KeyCode.G))
            Drop();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void PickUp(Item item)
    {
        // Add to inventory
        AllItems.Add(item);

        // Auto-equip if nothing held
        if (HeldItem == null)
            Equip(item);

        item.OnPickup(this);
        OnItemPickedUp?.Invoke(item);
    }

    public void Equip(Item item)
    {
        if (HeldItem != null)
            HeldItem.OnUnequip(this);

        HeldItem = item;

        // Destroy old visual
        if (_heldVisual != null) Destroy(_heldVisual);

        // If item has a visual prefab, spawn it at hold point
        // (Item subclasses can override OnPickup to spawn their own visual)
    }

    public void Drop()
    {
        if (HeldItem == null) return;
        HeldItem.gameObject.SetActive(true);

        HeldItem.OnUnequip(this);
        OnItemDropped?.Invoke(HeldItem);
        AllItems.Remove(HeldItem);
        HeldItem = null;

        if (_heldVisual != null)
        {
            Destroy(_heldVisual);
            _heldVisual = null;
        }
    }

    /// <summary>Check if the player currently holds an item of a specific type.</summary>
    public bool IsHolding<T>() where T : Item => HeldItem is T;

    /// <summary>Check if the player holds any item with a specific name.</summary>
    public bool IsHoldingItem(string name) =>
        HeldItem != null && HeldItem.itemName == name;
}
