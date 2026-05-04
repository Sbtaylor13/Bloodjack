using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private BlackjackGame _game;
    private int _pendingBet;

    public void Init(BlackjackGame game)
    {
        _game = game;
        _pendingBet = game.minBet;

        game.OnStateChanged += HandleStateChange;
        game.OnGameOver += HandleGameOver;
        game.OnCardDealt += (_, __) => RefreshScores();

        betPlusButton?.onClick.AddListener(() => AdjustBet(+game.minBet));
        betMinusButton?.onClick.AddListener(() => AdjustBet(-game.minBet));
        dealButton?.onClick.AddListener(OnDealClicked);

        hitButton?.onClick.AddListener(game.Hit);
        standButton?.onClick.AddListener(game.Stand);
        doubleButton?.onClick.AddListener(() => {
            game.DoubleDown();
            RefreshBetDisplay();
            RefreshChips();
        });

        nextRoundButton?.onClick.AddListener(OnNextRoundClicked);
        leaveTableButton?.onClick.AddListener(OnLeaveClicked);

        ShowOnly(bettingPanel);
        RefreshBetDisplay();
        RefreshChips();
    }
    void Start()
    {
        if (_game != null)
        {
            RefreshChips();
            RefreshBetDisplay();
            RefreshScores();
        }
    }
    void OnDestroy()
    {
        if (_game == null) return;
        _game.OnStateChanged -= HandleStateChange;
        _game.OnGameOver -= HandleGameOver;
    }

    // ── State Handling ────────────────────────────────────────────────────────

    void HandleStateChange(BlackjackGame.GameState state)
    {
        switch (state)
        {
            case BlackjackGame.GameState.Dealing:
                // Hide everything while cards are being dealt
                ShowOnly(null);
                resultBanner?.SetActive(false);
                break;

            case BlackjackGame.GameState.PlayerTurn:
                // All 4 cards are dealt — now show action buttons
                ShowOnly(actionPanel);
                RefreshActionButtons();
                break;

            case BlackjackGame.GameState.DealerTurn:
                // Hide action buttons while dealer plays
                ShowOnly(null);
                break;

            case BlackjackGame.GameState.Idle:
                ShowOnly(bettingPanel);
                resultBanner?.SetActive(false);
                RefreshBetDisplay();
                RefreshChips();
                break;
        }
        RefreshScores();
    }

    void HandleGameOver(BlackjackGame.GameResult result, int payout)
    {
        ShowOnly(postRoundPanel);
        resultBanner?.SetActive(true);

        if (resultText != null)
        {
            resultText.text = result switch
            {
                BlackjackGame.GameResult.PlayerBlackjack => "✦ BLACKJACK! ✦",
                BlackjackGame.GameResult.PlayerWin => "You Win!",
                BlackjackGame.GameResult.DealerBust => "Dealer Busts — You Win!",
                BlackjackGame.GameResult.Push => "Push — Bet Returned",
                BlackjackGame.GameResult.PlayerBust => "Bust!",
                BlackjackGame.GameResult.DealerWin => "Dealer Wins",
                _ => ""
            };
        }

        RefreshChips();
        RefreshScores();
    }

    // ── Panel Control ─────────────────────────────────────────────────────────

    /// <summary>
    /// Shows exactly one panel, hides all others.
    /// Pass null to hide everything (e.g. during dealing or dealer turn).
    /// Result banner is intentionally NOT controlled here.
    /// </summary>
    void ShowOnly(GameObject panel)
    {
        bettingPanel?.SetActive(bettingPanel == panel);
        actionPanel?.SetActive(actionPanel == panel);
        postRoundPanel?.SetActive(postRoundPanel == panel);
    }

    // ── Betting ───────────────────────────────────────────────────────────────

    void AdjustBet(int delta)
    {
        _pendingBet = Mathf.Clamp(
            _pendingBet + delta,
            _game.minBet,
            Mathf.Min(_game.maxBet, _game.PlayerChips)
        );
        RefreshBetDisplay();
    }

    void OnDealClicked()
    {
        _game.dealer?.ClearTable();
        _game.PlaceBet(_pendingBet);
        _game.StartRound();
        RefreshBetDisplay(); // add this line
        RefreshChips();      // and this — chips deducted on deal
    }

    void OnNextRoundClicked()
    {
        _game.dealer?.ClearTable();
        _game.ExitTable();
        _pendingBet = Mathf.Clamp(_pendingBet, _game.minBet, _game.PlayerChips);
        ShowOnly(bettingPanel);
        resultBanner?.SetActive(false);
        RefreshBetDisplay();
        RefreshChips();
    }

    void OnLeaveClicked()
    {
        _game.ExitTable();
        DeskInteractable.OnLeaveTable?.Invoke();
    }

    // ── Display ───────────────────────────────────────────────────────────────

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

        if (playerScoreLabel != null)
            playerScoreLabel.text = _game.PlayerHand.Count > 0
                ? BlackjackGame.HandValue(_game.PlayerHand).ToString()
                : "";

        if (dealerScoreLabel != null)
        {
            if (_game.DealerHand.Count == 0)
                dealerScoreLabel.text = "";
            else if (_game.State == BlackjackGame.GameState.PlayerTurn ||
                     _game.State == BlackjackGame.GameState.Dealing)
                // Only show the face-up card value while hole card is hidden
                dealerScoreLabel.text = _game.DealerHand[0].BlackjackValue.ToString();
            else
                dealerScoreLabel.text = BlackjackGame.HandValue(_game.DealerHand).ToString();
        }
    }

    void RefreshActionButtons()
    {
        if (_game == null) return;
        bool canDouble = _game.PlayerHand.Count == 2 && _game.PlayerChips >= _game.CurrentBet;
        if (doubleButton != null) doubleButton.interactable = canDouble;
    }

    public void HideAll() => ShowOnly(null);
}