using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the 2D HUD: chips, bet, score labels, action buttons, result banner.
/// Wire up all references in the Inspector.
/// </summary>
public class BlackjackUI : MonoBehaviour
{
    [Header("Info Labels")]
    public TMP_Text playerChipsLabel;
    public TMP_Text currentBetLabel;
    public TMP_Text playerScoreLabel;
    public TMP_Text dealerScoreLabel;

    [Header("Result Banner")]
    public GameObject resultBanner;
    public TMP_Text resultText;

    [Header("Betting Panel")]
    public GameObject bettingPanel;
    public TMP_Text betAmountText;
    public Button betPlusButton;
    public Button betMinusButton;
    public Button dealButton;

    [Header("Action Panel")]
    public GameObject actionPanel;
    public Button hitButton;
    public Button standButton;
    public Button doubleButton;

    [Header("Post-Round Panel")]
    public GameObject postRoundPanel;
    public Button nextRoundButton;
    public Button leaveTableButton;

    // ─────────────────────────────────────────────────────────────────────────

    private BlackjackGame _game;
    private int _pendingBet;

    public void Init(BlackjackGame game)
    {
        _game = game;
        _pendingBet = game.minBet;

        // Wire game events
        game.OnStateChanged  += HandleStateChange;
        game.OnGameOver      += HandleGameOver;
        game.OnCardDealt     += (_, isPlayer) => RefreshScores();

        // Wire betting buttons
        betPlusButton?.onClick.AddListener(() => AdjustBet(+game.minBet));
        betMinusButton?.onClick.AddListener(() => AdjustBet(-game.minBet));
        dealButton?.onClick.AddListener(OnDealClicked);

        // Wire action buttons
        hitButton?.onClick.AddListener(game.Hit);
        standButton?.onClick.AddListener(game.Stand);
        doubleButton?.onClick.AddListener(game.DoubleDown);

        // Wire post-round
        nextRoundButton?.onClick.AddListener(OnNextRoundClicked);
        leaveTableButton?.onClick.AddListener(OnLeaveClicked);

        ShowBettingPanel();
        RefreshChips();
    }

    void OnDestroy()
    {
        if (_game == null) return;
        _game.OnStateChanged -= HandleStateChange;
        _game.OnGameOver     -= HandleGameOver;
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    void HandleStateChange(BlackjackGame.GameState state)
    {
        switch (state)
        {
            case BlackjackGame.GameState.PlayerTurn:
                bettingPanel?.SetActive(false);
                resultBanner?.SetActive(false);
                actionPanel?.SetActive(true);
                postRoundPanel?.SetActive(false);
                RefreshActionButtons();
                break;

            case BlackjackGame.GameState.DealerTurn:
                actionPanel?.SetActive(false);
                break;

            case BlackjackGame.GameState.Idle:
                ShowBettingPanel();
                break;
        }
        RefreshScores();
    }

    void HandleGameOver(BlackjackGame.GameResult result, int payout)
    {
        actionPanel?.SetActive(false);
        postRoundPanel?.SetActive(true);

        string msg = result switch
        {
            BlackjackGame.GameResult.PlayerBlackjack => "✦ BLACKJACK! ✦",
            BlackjackGame.GameResult.PlayerWin        => "You Win!",
            BlackjackGame.GameResult.DealerBust       => "Dealer Busts — You Win!",
            BlackjackGame.GameResult.Push             => "Push — Bet Returned",
            BlackjackGame.GameResult.PlayerBust       => "Bust!",
            BlackjackGame.GameResult.DealerWin        => "Dealer Wins",
            _                                         => ""
        };

        if (resultText != null) resultText.text = msg;
        resultBanner?.SetActive(true);
        RefreshChips();
        RefreshScores();
    }

    // ── Betting ───────────────────────────────────────────────────────────────

    void AdjustBet(int delta)
    {
        _pendingBet = Mathf.Clamp(_pendingBet + delta, _game.minBet,
            Mathf.Min(_game.maxBet, _game.PlayerChips));
        RefreshBetDisplay();
    }

    void OnDealClicked()
    {
        _game.PlaceBet(_pendingBet);
        _game.StartRound();
        _game.dealer?.ClearTable();
    }

    void OnNextRoundClicked()
    {
        postRoundPanel?.SetActive(false);
        resultBanner?.SetActive(false);
        _pendingBet = Mathf.Min(_pendingBet, _game.PlayerChips); // clamp if low on chips
        ShowBettingPanel();
    }

    void OnLeaveClicked()
    {
        _game.ExitTable();
        // Raise event or call your scene manager to return to the 3D room
        // E.g.: SceneManager.LoadScene("MainRoom");
        Debug.Log("[BlackjackUI] Player left the table.");
    }

    // ── Display Helpers ───────────────────────────────────────────────────────

    void ShowBettingPanel()
    {
        bettingPanel?.SetActive(true);
        actionPanel?.SetActive(false);
        postRoundPanel?.SetActive(false);
        RefreshBetDisplay();
        RefreshChips();
    }

    void RefreshBetDisplay()
    {
        if (betAmountText != null) betAmountText.text = $"${_pendingBet}";
        if (currentBetLabel != null) currentBetLabel.text = $"Bet: ${_game.CurrentBet}";
    }

    void RefreshChips()
    {
        if (playerChipsLabel != null)
            playerChipsLabel.text = $"${_game.PlayerChips}";
    }

    void RefreshScores()
    {
        if (_game == null) return;

        int pVal = BlackjackGame.HandValue(_game.PlayerHand);
        if (playerScoreLabel != null)
            playerScoreLabel.text = _game.PlayerHand.Count > 0 ? pVal.ToString() : "";

        // Dealer: only show first card's value until dealer turn
        if (dealerScoreLabel != null)
        {
            if (_game.DealerHand.Count == 0)
            {
                dealerScoreLabel.text = "";
            }
            else if (_game.State == BlackjackGame.GameState.PlayerTurn)
            {
                // Show only the value of the face-up card
                var faceUp = _game.DealerHand[0];
                dealerScoreLabel.text = faceUp.BlackjackValue.ToString();
            }
            else
            {
                dealerScoreLabel.text = BlackjackGame.HandValue(_game.DealerHand).ToString();
            }
        }
    }

    void RefreshActionButtons()
    {
        if (_game == null) return;
        bool canDouble = _game.PlayerHand.Count == 2 && _game.PlayerChips >= _game.CurrentBet;
        if (doubleButton != null) doubleButton.interactable = canDouble;
    }

    public void HideAll()
    {
        bettingPanel?.SetActive(false);
        actionPanel?.SetActive(false);
        postRoundPanel?.SetActive(false);
        resultBanner?.SetActive(false);
    }
}
