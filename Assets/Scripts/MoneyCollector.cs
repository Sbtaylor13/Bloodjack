using UnityEngine;

/// <summary>
/// Attach to the Player GameObject alongside PlayerInventory.
/// Handles clicking MoneyCubes while holding the scalpel.
/// Stores the collected total so BlackjackGame can read it on scene load.
/// </summary>
public class MoneyCollector : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory inventory;

    [Header("Settings")]
    public float collectRange = 4f;
    public LayerMask cubeLayerMask = ~0;

    [Header("Feedback")]
    public TMPro.TMP_Text collectedMoneyUI;  // optional HUD label showing total collected

    public static int CollectedChips = 0;

    private Camera _cam;

    void Awake()
    {
        _cam = GetComponentInChildren<Camera>();
        if (_cam == null) _cam = Camera.main;

        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();
    }

    void Update()
    {
        if (!inventory.IsHoldingItem("Scalpel")) return;

        if (Input.GetMouseButtonDown(0))
            TryCollect();

        RefreshUI();
    }

    void TryCollect()
    {
        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, collectRange, cubeLayerMask)) return;

        MoneyCube cube = hit.collider.GetComponent<MoneyCube>();
        if (cube == null) return;

        Collect(cube);
    }

    void Collect(MoneyCube cube)
    {
        CollectedChips += cube.value;
        Debug.Log($"[MoneyCollector] Collected ${cube.value}. Total: ${CollectedChips}");

        // Spawn effect if assigned
        if (cube.collectEffectPrefab != null)
            Instantiate(cube.collectEffectPrefab, cube.transform.position, Quaternion.identity);

        Destroy(cube.gameObject);
        RefreshUI();
    }

    void RefreshUI()
    {
        if (collectedMoneyUI != null)
            collectedMoneyUI.text = $"Chips: ${CollectedChips}";
    }

    /// <summary>
    /// Call this to reset collected chips (e.g. when starting a new game session).
    /// </summary>
    public static void ResetChips() => CollectedChips = 0;
}
