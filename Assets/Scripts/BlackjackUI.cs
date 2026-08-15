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

    [Header("Starting Panel")]
    public GameObject StartingPanel;
    public Button AcceptDeal;
    public Button leaveTableButt;
    public TMP_Text DealerMessage;

    private BlackjackGame _game;
    private int _pendingBet;
    private bool _startingPanelShown = false;

    private string[] messages = new[]
    {
        "A fine assortment of parts. I can offer ",
        "Marvelous, I'll give you ",
        "That's all? Fine, you'll get ",
        "Your patient is quite the specimen. I can offer ",
        "Best I can do is ",
        "For these parts, ",
        "I can offer ",
        "You still think you can help him? Hilarious. I'll give you ",
        "Putting your Medical Degree to good use I see. I can offer "
    };
    private string endMessage = " if you win.";

    // ── Init ──────────────────────────────────────────────────────────────────

    public void Init(BlackjackGame game)
    {
        // Hide everything first
        ShowOnly(null);
        StartingPanel?.SetActive(false);
        resultBanner?.SetActive(false);

        _game = game;
        _pendingBet = game.minBet;

        game.OnStateChanged += HandleStateChange;
        game.OnGameOver     += HandleGameOver;
        game.OnCardDealt    += (_, __) => RefreshScores();

        betPlusButton?.onClick.AddListener(() => AdjustBet(+game.minBet));
        betMinusButton?.onClick.AddListener(() => AdjustBet(-game.minBet));
        dealButton?.onClick.AddListener(OnDealClicked);

        hitButton?.onClick.AddListener(game.Hit);
        standButton?.onClick.AddListener(game.Stand);
        doubleButton?.onClick.AddListener(() =>
        {
            game.DoubleDown();
            RefreshBetDisplay();
            RefreshChips();
        });

        nextRoundButton?.onClick.AddListener(OnNextRoundClicked);
        leaveTableButton?.onClick.AddListener(OnLeaveClicked);

        // Starting panel buttons wired once here
        AcceptDeal?.onClick.AddListener(() =>
        {
            StartingPanel?.SetActive(false);
            _startingPanelShown = true;
            ShowOnly(bettingPanel);
            RefreshBetDisplay();
            RefreshChips();
        });

        leaveTableButt?.onClick.AddListener(() =>
        {
            StartingPanel?.SetActive(false);
            DeskInteractable.OnLeaveTable?.Invoke();
        });

        RefreshBetDisplay();
        RefreshChips();
        ShowStartingPanel();
    }

    /// <summary>Called by DeskInteractable every time the player re-opens the table.</summary>
    public void OnReenter()
    {
        _game.SyncChips();
        RefreshChips();
        RefreshBetDisplay();

        if (!_startingPanelShown)
        {
            ShowStartingPanel();
        }
        else
        {
            resultBanner?.SetActive(false);
            _game.ExitTable();
            ShowOnly(bettingPanel);
        }
    }

    void ShowStartingPanel()
    {
        ShowOnly(null);
        resultBanner?.SetActive(false);
        StartingPanel?.SetActive(true);

        if (MoneyCollector.CollectedChips > 0)
        {
            AcceptDeal?.gameObject.SetActive(true);
            DealerMessage.text = messages[Random.Range(0, messages.Length)] + $"${_game.maxBet}" + endMessage;
        }
        else
        {
            AcceptDeal?.gameObject.SetActive(false);
            DealerMessage.text = "Come back when you have something to offer.";
        }
    }

    void OnDestroy()
    {
        if (_game == null) return;
        _game.OnStateChanged -= HandleStateChange;
        _game.OnGameOver     -= HandleGameOver;
    }

    // ── State Handling ────────────────────────────────────────────────────────

    void HandleStateChange(BlackjackGame.GameState state)
    {
        switch (state)
        {
            case BlackjackGame.GameState.Dealing:
                ShowOnly(null);
                resultBanner?.SetActive(false);
                break;
            case BlackjackGame.GameState.PlayerTurn:
                ShowOnly(actionPanel);
                RefreshActionButtons();
                break;
            case BlackjackGame.GameState.DealerTurn:
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
                BlackjackGame.GameResult.PlayerWin        => "You Win!",
                BlackjackGame.GameResult.DealerBust       => "Dealer Busts — You Win!",
                BlackjackGame.GameResult.Push             => "Push — Bet Returned",
                BlackjackGame.GameResult.PlayerBust       => "Bust!",
                BlackjackGame.GameResult.DealerWin        => "Dealer Wins",
                _                                         => ""
            };
        }

        RefreshChips();
        RefreshScores();
    }

    // ── Panel Control ─────────────────────────────────────────────────────────

    void ShowOnly(GameObject panel)
    {
        bettingPanel?.SetActive(bettingPanel     == panel);
        actionPanel?.SetActive(actionPanel       == panel);
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
        RefreshBetDisplay();
        RefreshChips();
    }

    void OnNextRoundClicked()
    {
        _game.dealer?.ClearTable();
        _game.ExitTable();
        _pendingBet = Mathf.Clamp(_pendingBet, _game.minBet, _game.PlayerChips);
        resultBanner?.SetActive(false);
        ShowOnly(bettingPanel);
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
        if (betAmountText   != null) betAmountText.text   = $"${_pendingBet}";
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
