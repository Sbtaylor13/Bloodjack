using UnityEngine;

/// <summary>
/// Attach to each money cube on the table.
/// Player must be holding the scalpel and click the cube to collect it.
/// </summary>
public class MoneyCube : MonoBehaviour
{
    [Header("Value")]
    [Tooltip("How much this cube is worth in blackjack chips")]
    public int value = 50;

    [Header("Interaction")]
    [Tooltip("Max distance the player can be to click this cube")]
    public float collectRange = 4f;

    [Header("Visual Feedback")]
    [Tooltip("Optional particle effect prefab spawned on collect")]
    public GameObject collectEffectPrefab;

    // Show value in Scene view label
    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, $"${value}");
#endif
    }
}
