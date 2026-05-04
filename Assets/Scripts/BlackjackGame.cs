using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackjackGame : MonoBehaviour
{
    public enum GameState { Idle, Dealing, PlayerTurn, DealerTurn, GameOver }
    public enum GameResult { None, PlayerWin, DealerWin, Push, PlayerBlackjack, PlayerBust, DealerBust }

    [Header("References")]
    public BlackjackDealer dealer;
    public BlackjackUI ui;

    [Header("Settings")]
    public int numberOfDecks = 2;
    public int playerStartChips = 500;
    public int minBet = 10;
    public int maxBet = 500;
    public float dealDelay = 0.4f;

    public GameState State { get; private set; } = GameState.Idle;
    public GameResult LastResult { get; private set; } = GameResult.None;
    public int PlayerChips { get; private set; }
    public int CurrentBet { get; private set; }

    public List<Card> PlayerHand { get; private set; } = new();
    public List<Card> DealerHand { get; private set; } = new();

    private List<Card> _deck = new();

    public System.Action<GameState> OnStateChanged;
    public System.Action<GameResult, int> OnGameOver;
    public System.Action OnHandsDealt;
    public System.Action<Card, bool> OnCardDealt;

    void Awake() => PlayerChips = playerStartChips;

    // ── Public API ────────────────────────────────────────────────────────────

    public void PlaceBet(int amount)
    {
        if (State != GameState.Idle) return;
        CurrentBet = Mathf.Clamp(amount, minBet, Mathf.Min(maxBet, PlayerChips));
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
        PlayerHand.Clear();
        DealerHand.Clear();
        // Do NOT call OnStateChanged here — UI handles its own reset
    }

    // ── Dealing ───────────────────────────────────────────────────────────────

    IEnumerator DealOpeningHands()
    {
        // Use Dealing state — UI shows nothing during this phase
        SetState(GameState.Dealing);

        yield return StartCoroutine(DealCardTo(PlayerHand, faceUp: true, isPlayer: true));
        yield return StartCoroutine(DealCardTo(DealerHand, faceUp: true, isPlayer: false));
        yield return StartCoroutine(DealCardTo(PlayerHand, faceUp: true, isPlayer: true));
        yield return StartCoroutine(DealCardTo(DealerHand, faceUp: false, isPlayer: false));

        // All 4 cards dealt — NOW check state
        OnHandsDealt?.Invoke();

        if (HandValue(PlayerHand) == 21)
        {
            yield return StartCoroutine(RevealHoleCard());
            EndGame(HandValue(DealerHand) == 21 ? GameResult.Push : GameResult.PlayerBlackjack);
        }
        else
        {
            // Only NOW tell the UI the player can act
            SetState(GameState.PlayerTurn);
        }
    }

    IEnumerator PlayerHit()
    {
        yield return StartCoroutine(DealCardTo(PlayerHand, faceUp: true, isPlayer: true));

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
        yield return StartCoroutine(DealCardTo(PlayerHand, faceUp: true, isPlayer: true));

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

        while (HandValue(DealerHand) < 17 || IsSoft17(DealerHand))
        {
            yield return StartCoroutine(DealCardTo(DealerHand, faceUp: true, isPlayer: false));
            yield return new WaitForSeconds(0.3f);
        }

        int playerVal = HandValue(PlayerHand);
        int dealerVal = HandValue(DealerHand);

        if (dealerVal > 21) EndGame(GameResult.DealerBust);
        else if (playerVal > dealerVal) EndGame(GameResult.PlayerWin);
        else if (dealerVal > playerVal) EndGame(GameResult.DealerWin);
        else EndGame(GameResult.Push);
    }

    IEnumerator RevealHoleCard()
    {
        if (DealerHand.Count >= 2)
        {
            DealerHand[1].IsFaceUp = true;
            dealer?.RevealHoleCard(DealerHand[1]);
            OnCardDealt?.Invoke(DealerHand[1], false);
        }
        yield return new WaitForSeconds(0.35f);
    }

    IEnumerator DealCardTo(List<Card> hand, bool faceUp, bool isPlayer)
    {
        Card card = DrawCard();
        card.IsFaceUp = faceUp;
        hand.Add(card);

        dealer?.AnimateDeal(card, isPlayer, hand.Count - 1);
        OnCardDealt?.Invoke(card, isPlayer);

        yield return new WaitForSeconds(dealDelay);
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

    int CalculatePayout(GameResult result) => result switch
    {
        GameResult.PlayerBlackjack => Mathf.RoundToInt(CurrentBet * 2.5f),
        GameResult.PlayerWin => CurrentBet * 2,
        GameResult.DealerBust => CurrentBet * 2,
        GameResult.Push => CurrentBet,
        _ => 0,
    };

    // ── Hand Value ────────────────────────────────────────────────────────────

    public static int HandValue(List<Card> hand)
    {
        int value = 0, aces = 0;
        foreach (var card in hand)
        {
            value += card.BlackjackValue;
            if (card.Rank == CardRank.Ace) aces++;
        }
        while (value > 21 && aces > 0) { value -= 10; aces--; }
        return value;
    }

    public static bool IsSoft(List<Card> hand)
    {
        int value = 0, aces = 0;
        foreach (var c in hand) { value += c.BlackjackValue; if (c.Rank == CardRank.Ace) aces++; }
        return aces > 0 && value <= 21;
    }

    static bool IsSoft17(List<Card> hand) => HandValue(hand) == 17 && IsSoft(hand);

    // ── Deck ──────────────────────────────────────────────────────────────────

    void EnsureDeckHasCards() { if (_deck.Count < 15) BuildAndShuffleDeck(); }

    void BuildAndShuffleDeck()
    {
        _deck.Clear();
        for (int d = 0; d < numberOfDecks; d++)
            foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
                foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
                    _deck.Add(new Card(suit, rank));
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }

    Card DrawCard()
    {
        if (_deck.Count == 0) BuildAndShuffleDeck();
        Card c = _deck[0]; _deck.RemoveAt(0); return c;
    }

    void SetState(GameState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);
    }
}