using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that maps card names → sprites.
/// Create via Assets > Create > Blackjack > Card Sprite Library.
/// Drag all your card sprites into the list; they must be named exactly
/// like Card.SpriteName returns: e.g. "Hearts_Queen", "Spades_Ace", etc.
/// The card back sprite is used for face-down cards.
/// </summary>
[CreateAssetMenu(menuName = "Blackjack/Card Sprite Library")]
public class CardSpriteLibrary : ScriptableObject
{
    [Tooltip("All card face sprites. Name them Suit_Rank, e.g. Hearts_Queen")]
    public List<Sprite> CardFaces = new();

    [Tooltip("Sprite shown when a card is face-down (the hole card)")]
    public Sprite CardBack;

    private Dictionary<string, Sprite> _lookup;

    void OnEnable() => BuildLookup();

    void BuildLookup()
    {
        _lookup = new Dictionary<string, Sprite>();
        foreach (var sprite in CardFaces)
            if (sprite != null)
                _lookup[sprite.name] = sprite;
    }

    public Sprite GetSprite(Card card)
    {
        if (_lookup == null) BuildLookup();

        if (!card.IsFaceUp) return CardBack;

        if (_lookup.TryGetValue(card.SpriteName, out var sprite))
            return sprite;

        Debug.LogWarning($"[CardSpriteLibrary] No sprite found for '{card.SpriteName}'. " +
                         "Make sure your sprites are named Suit_Rank (e.g. Hearts_Queen).");
        return CardBack;
    }
}
