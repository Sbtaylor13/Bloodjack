using UnityEngine;

public enum CardSuit { Hearts, Diamonds, Clubs, Spades }
public enum CardRank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

[System.Serializable]
public class Card
{
    public CardSuit Suit;
    public CardRank Rank;
    public bool IsFaceUp = true;

    public Card(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    /// <summary>Blackjack value: face cards = 10, Ace = 11 (soft), numbers = face value.</summary>
    public int BlackjackValue => Rank switch
    {
        CardRank.Ace   => 11,
        CardRank.King  => 10,
        CardRank.Queen => 10,
        CardRank.Jack  => 10,
        _              => (int)Rank
    };

    /// <summary>Returns the sprite name to look up in your CardSpriteLibrary, e.g. "Hearts_Queen".</summary>
    public string SpriteName => $"{Suit}_{Rank}";

    public override string ToString() => $"{Rank} of {Suit}";
}
