using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackjackDealer : MonoBehaviour
{
    [Header("Prefab & Library")]
    public GameObject cardPrefab;
    public CardSpriteLibrary spriteLibrary;

    [Header("Layout")]
    public Transform dealerCardOrigin;
    public Transform playerCardOrigin;
    public float cardSpacing = 6.1f;

    [Header("Deal Animation")]
    public float dealDuration = 0.25f;
    public Transform deckPosition;

    // Separate lists per hand so recentering is always accurate
    private List<GameObject> _playerCards = new();
    private List<GameObject> _dealerCards = new();

    void Start()
    {
        if (spriteLibrary == null)
            Debug.LogError("[BlackjackDealer] spriteLibrary is not assigned!");
        else
            Debug.Log($"[BlackjackDealer] Library loaded: {spriteLibrary.CardFaces.Count} sprites.");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void AnimateDeal(Card card, bool isPlayer, int slotIndex)
    {
        if (cardPrefab == null) { Debug.LogError("cardPrefab is null!"); return; }

        Transform origin = isPlayer ? playerCardOrigin : dealerCardOrigin;
        if (origin == null) { Debug.LogError($"Origin is null! isPlayer:{isPlayer}"); return; }

        GameObject go = Instantiate(cardPrefab, GetStartPos(), origin.rotation);
        go.transform.SetParent(transform);
        go.transform.localScale = origin.localScale;
        go.name = card.IsFaceUp ? card.SpriteName : "HoleCard";

        Debug.Log($"Spawned {go.name} at {go.transform.position}, scale {go.transform.localScale}, rot {go.transform.rotation.eulerAngles}");

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && spriteLibrary != null)
            sr.sprite = spriteLibrary.GetSprite(card);
        else
            Debug.LogError($"SpriteRenderer null: {sr == null}, Library null: {spriteLibrary == null}");

        if (isPlayer) _playerCards.Add(go);
        else _dealerCards.Add(go);

        RecenterHand(isPlayer);
    }

    public void RevealHoleCard(Card holeCard)
    {
        foreach (var go in _dealerCards)
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

    public void ClearTable()
    {
        StopAllCoroutines();
        var all = new List<GameObject>(_playerCards);
        all.AddRange(_dealerCards);
        foreach (var go in all)
            if (go != null) Destroy(go);
        _playerCards.Clear();
        _dealerCards.Clear();
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Repositions all cards in a hand so they are always centered on their origin.
    /// Called every time a new card is added.
    /// </summary>
    void RecenterHand(bool isPlayer)
    {
        List<GameObject> hand = isPlayer ? _playerCards : _dealerCards;
        Transform origin = isPlayer ? playerCardOrigin : dealerCardOrigin;

        int count = hand.Count;
        if (count == 0) return;

        float totalWidth = (count - 1) * cardSpacing;

        for (int i = 0; i < count; i++)
        {
            if (hand[i] == null) continue;

            hand[i].transform.rotation = origin.rotation;
            hand[i].transform.localScale = origin.localScale;

            // Spread along origin's local RIGHT axis, centered on origin
            float offset = -totalWidth / 2f + i * cardSpacing;
            Vector3 target = origin.position + origin.right * offset;

            StartCoroutine(MoveCard(hand[i].transform, target, dealDuration));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    Vector3 GetStartPos()
    {
        if (deckPosition != null)
            return new Vector3(deckPosition.position.x, deckPosition.position.y, deckPosition.position.z);
        return new Vector3(10f, 0f, 0f); // fallback — adjust to match your table
    }

    // ── Animations ────────────────────────────────────────────────────────────

    IEnumerator MoveCard(Transform t, Vector3 target, float duration)
    {
        if (t == null) yield break;
        Vector3 start = t.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (t == null) yield break;
            elapsed += Time.deltaTime;
            t.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        if (t != null) t.position = target;
    }

    IEnumerator FlipCard(GameObject go)
    {
        float half = 0.1f;
        var t = go.transform;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            t.localScale = new Vector3(1f - (elapsed / half), 1f, 1f);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            t.localScale = new Vector3(elapsed / half, 1f, 1f);
            yield return null;
        }

        t.localScale = Vector3.one;
    }
}