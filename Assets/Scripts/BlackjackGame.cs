using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core blackjack game logic. Attach to a persistent manager object in your subscene.
/// </summary>
public class BlackjackGame : MonoBehaviour
{
    public enum GameState { Idle, PlayerTurn, DealerTurn, GameOver }
    public enum GameResult { None, PlayerWin, DealerWin, Push, PlayerBlackjack, PlayerBust, DealerBust }

    [Header("References")]
    public BlackjackDealer dealer;
    public BlackjackUI ui;

    [Header("Settings")]
    public int numberOfDecks = 2;
    public int playerStartChips = 500;
    public int minBet = 10;
    public int maxBet = 500;

    // Game state
    public GameState State { get; private set; } = GameState.Idle;
    public GameResult LastResult { get; private set; } = GameResult.None;
    public int PlayerChips { get; private set; }
    public int CurrentBet { get; private set; }

    // Hands
    public List<Card> PlayerHand { get; private set; } = new();
    public List<Card> DealerHand { get; private set; } = new();

    private List<Card> _deck = new();

    // ── Events ────────────────────────────────────────────────────────────────
    public System.Action<GameState> OnStateChanged;
    public System.Action<GameResult, int> OnGameOver;   // result, payout
    public System.Action OnHandsDealt;
    public System.Action<Card, bool> OnCardDealt;       // card, isPlayer

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        PlayerChips = playerStartChips;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void PlaceBet(int amount)
    {
        if (State != GameState.Idle) return;
        amount = Mathf.Clamp(amount, minBet, Mathf.Min(maxBet, PlayerChips));
        CurrentBet = amount;
    }

    public void StartRound()
    {
        if (State != GameState.Idle || CurrentBet <= 0) return;
        if (PlayerChips < CurrentBet) { Debug.LogWarning("Not enough chips."); return; }

        PlayerChips -= CurrentBet;
        PlayerHand.Clear();
        DealerHand.Clear();

        EnsureDeckHasCards();
        StartCoroutine(DealOpeningHands());
    }

    public void Hit()
    {
        if (State != GameState.PlayerTurn) return;
        StartCoroutine(PlayerHit());
    }

    public void Stand()
    {
        if (State != GameState.PlayerTurn) return;
        StartCoroutine(DealerPlay());
    }

    public void DoubleDown()
    {
        if (State != GameState.PlayerTurn) return;
        if (PlayerHand.Count != 2) return;
        if (PlayerChips < CurrentBet) return;

        PlayerChips -= CurrentBet;
        CurrentBet *= 2;
        StartCoroutine(PlayerDoubleDown());
    }

    public void ExitTable()
    {
        State = GameState.Idle;
        CurrentBet = 0;
        ui?.HideAll();
    }

    // ── Card Dealing Coroutines ───────────────────────────────────────────────

    IEnumerator DealOpeningHands()
    {
        SetState(GameState.PlayerTurn); // set early so UI shows

        // p1, d1, p2, d2 (dealer's second card face-down)
        yield return DealCardTo(PlayerHand, faceUp: true, isPlayer: true);
        yield return new WaitForSeconds(0.3f);
        yield return DealCardTo(DealerHand, faceUp: true, isPlayer: false);
        yield return new WaitForSeconds(0.3f);
        yield return DealCardTo(PlayerHand, faceUp: true, isPlayer: true);
        yield return new WaitForSeconds(0.3f);
        yield return DealCardTo(DealerHand, faceUp: false, isPlayer: false); // hole card
        yield return new WaitForSeconds(0.3f);

        OnHandsDealt?.Invoke();

        // Check immediate blackjack
        if (HandValue(PlayerHand) == 21)
        {
            yield return StartCoroutine(RevealHoleCard());
            if (HandValue(DealerHand) == 21)
                EndGame(GameResult.Push);
            else
                EndGame(GameResult.PlayerBlackjack);
        }
        else
        {
            SetState(GameState.PlayerTurn);
        }
    }

    IEnumerator PlayerHit()
    {
        yield return DealCardTo(PlayerHand, faceUp: true, isPlayer: true);
        yield return new WaitForSeconds(0.4f);

        int val = HandValue(PlayerHand);
        if (val > 21)
        {
            yield return StartCoroutine(RevealHoleCard());
            EndGame(GameResult.PlayerBust);
        }
        else if (val == 21)
        {
            yield return StartCoroutine(DealerPlay());
        }
    }

    IEnumerator PlayerDoubleDown()
    {
        yield return DealCardTo(PlayerHand, faceUp: true, isPlayer: true);
        yield return new WaitForSeconds(0.4f);

        int val = HandValue(PlayerHand);
        if (val > 21)
        {
            yield return StartCoroutine(RevealHoleCard());
            EndGame(GameResult.PlayerBust);
        }
        else
        {
            yield return StartCoroutine(DealerPlay());
        }
    }

    IEnumerator DealerPlay()
    {
        SetState(GameState.DealerTurn);

        yield return StartCoroutine(RevealHoleCard());
        yield return new WaitForSeconds(0.5f);

        // Dealer hits on soft 17
        while (HandValue(DealerHand) < 17 || (IsSoft17(DealerHand)))
        {
            yield return DealCardTo(DealerHand, faceUp: true, isPlayer: false);
            yield return new WaitForSeconds(0.5f);
        }

        int playerVal = HandValue(PlayerHand);
        int dealerVal = HandValue(DealerHand);

        if (dealerVal > 21)
            EndGame(GameResult.DealerBust);
        else if (playerVal > dealerVal)
            EndGame(GameResult.PlayerWin);
        else if (dealerVal > playerVal)
            EndGame(GameResult.DealerWin);
        else
            EndGame(GameResult.Push);
    }

    IEnumerator RevealHoleCard()
    {
        if (DealerHand.Count >= 2)
        {
            DealerHand[1].IsFaceUp = true;
            OnCardDealt?.Invoke(DealerHand[1], false);
        }
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator DealCardTo(List<Card> hand, bool faceUp, bool isPlayer)
    {
        Card card = DrawCard();
        card.IsFaceUp = faceUp;
        hand.Add(card);
        OnCardDealt?.Invoke(card, isPlayer);
        dealer?.AnimateDeal(card, isPlayer, hand.Count - 1);
        yield return new WaitForSeconds(0.25f);
    }

    // ── Game End ──────────────────────────────────────────────────────────────

    void EndGame(GameResult result)
    {
        LastResult = result;
        int payout = CalculatePayout(result);
        PlayerChips += payout;

        SetState(GameState.GameOver);
        OnGameOver?.Invoke(result, payout);
    }

    int CalculatePayout(GameResult result)
    {
        return result switch
        {
            GameResult.PlayerBlackjack => Mathf.RoundToInt(CurrentBet * 2.5f), // 3:2 + original bet
            GameResult.PlayerWin       => CurrentBet * 2,
            GameResult.DealerBust      => CurrentBet * 2,
            GameResult.Push            => CurrentBet,     // return bet
            _                          => 0,              // lose
        };
    }

    // ── Hand Value ────────────────────────────────────────────────────────────

    public static int HandValue(List<Card> hand)
    {
        int value = 0;
        int aces = 0;

        foreach (var card in hand)
        {
            if (!card.IsFaceUp && hand.IndexOf(card) != 0) continue; // skip face-down (hole card)
            value += card.BlackjackValue;
            if (card.Rank == CardRank.Ace) aces++;
        }

        // Count all cards (including face-down for internal logic)
        value = 0; aces = 0;
        foreach (var card in hand)
        {
            value += card.BlackjackValue;
            if (card.Rank == CardRank.Ace) aces++;
        }

        while (value > 21 && aces > 0)
        {
            value -= 10;
            aces--;
        }

        return value;
    }

    public static bool IsSoft(List<Card> hand)
    {
        int value = 0, aces = 0;
        foreach (var c in hand) { value += c.BlackjackValue; if (c.Rank == CardRank.Ace) aces++; }
        return aces > 0 && value <= 21 && (value - 10) >= 1;
    }

    static bool IsSoft17(List<Card> hand) => HandValue(hand) == 17 && IsSoft(hand);

    // ── Deck Management ───────────────────────────────────────────────────────

    void EnsureDeckHasCards()
    {
        if (_deck.Count < 15) BuildAndShuffleDeck();
    }

    void BuildAndShuffleDeck()
    {
        _deck.Clear();
        for (int d = 0; d < numberOfDecks; d++)
            foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
                foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
                    _deck.Add(new Card(suit, rank));
        ShuffleDeck();
    }

    void ShuffleDeck()
    {
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }

    Card DrawCard()
    {
        if (_deck.Count == 0) BuildAndShuffleDeck();
        Card c = _deck[0];
        _deck.RemoveAt(0);
        return c;
    }

    void SetState(GameState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);
    }
}
