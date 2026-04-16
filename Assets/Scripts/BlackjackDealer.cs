using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns and animates card GameObjects in the 2D subscene.
/// Attach to the Dealer root object.
/// </summary>
public class BlackjackDealer : MonoBehaviour
{
    [Header("Prefab & Library")]
    [Tooltip("A simple SpriteRenderer prefab for a single card")]
    public GameObject cardPrefab;
    public CardSpriteLibrary spriteLibrary;

    [Header("Layout")]
    [Tooltip("Where dealer cards anchor (top centre of play area)")]
    public Transform dealerCardOrigin;
    [Tooltip("Where player cards anchor (bottom centre of play area)")]
    public Transform playerCardOrigin;
    [Tooltip("Horizontal spacing between cards")]
    public float cardSpacing = 1.1f;

    [Header("Deal Animation")]
    public float dealDuration = 0.25f;
    [Tooltip("Cards fly in from this off-screen position")]
    public Transform deckPosition;

    // Track spawned card objects so we can clean them up
    private List<GameObject> _spawnedCards = new();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called by BlackjackGame when a card is dealt.</summary>
    public void AnimateDeal(Card card, bool isPlayer, int slotIndex)
    {
        if (cardPrefab == null) return;

        Transform origin = isPlayer ? playerCardOrigin : dealerCardOrigin;
        Vector3 targetPos = origin.position + Vector3.right * (slotIndex - 1.5f) * cardSpacing;

        GameObject go = Instantiate(cardPrefab, deckPosition ? deckPosition.position : targetPos + Vector3.up * 5f, Quaternion.identity);
        go.transform.SetParent(transform);

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && spriteLibrary != null)
            sr.sprite = spriteLibrary.GetSprite(card);

        // Tag the GO so we can swap sprite when hole card flips
        go.name = card.SpriteName;

        _spawnedCards.Add(go);
        StartCoroutine(MoveCard(go.transform, targetPos, dealDuration));
    }

    /// <summary>Call when the hole card is revealed to flip its sprite.</summary>
    public void RevealHoleCard(Card holeCard)
    {
        // Hole card is always dealer's second card GO (index 1 among dealer children)
        // We stored them in order, so find it by name (it was named "CardBack" equivalent)
        foreach (var go in _spawnedCards)
        {
            if (go == null) continue;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == spriteLibrary?.CardBack)
            {
                sr.sprite = spriteLibrary.GetSprite(holeCard);
                go.name = holeCard.SpriteName;
                StartCoroutine(FlipCard(go));
                break;
            }
        }
    }

    /// <summary>Destroy all card GameObjects. Call between rounds.</summary>
    public void ClearTable()
    {
        foreach (var go in _spawnedCards)
            if (go != null) Destroy(go);
        _spawnedCards.Clear();
    }

    // ── Animations ────────────────────────────────────────────────────────────

    IEnumerator MoveCard(Transform t, Vector3 target, float duration)
    {
        Vector3 start = t.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        t.position = target;
    }

    IEnumerator FlipCard(GameObject go)
    {
        // Quick scale-X flip tween
        float half = 0.1f;
        var t = go.transform;
        float elapsed = 0f;

        // Squish to 0 on X
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float s = 1f - (elapsed / half);
            t.localScale = new Vector3(s, 1f, 1f);
            yield return null;
        }

        // Expand back
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float s = elapsed / half;
            t.localScale = new Vector3(s, 1f, 1f);
            yield return null;
        }

        t.localScale = Vector3.one;
    }
}
