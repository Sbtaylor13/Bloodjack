using UnityEngine;

/// <summary>
/// Drop this on a single GameObject in your Blackjack subscene (or Canvas root).
/// It finds the other components and calls UI.Init() so you don't have to wire
/// every event by hand for each new scene load.
/// </summary>
public class BlackjackSceneBootstrapper : MonoBehaviour
{
    [Header("Wire these in Inspector (or leave null to auto-find)")]
    public BlackjackGame game;
    public BlackjackUI ui;
    public BlackjackDealer dealer;

    void Awake()
    {
        if (game   == null) game   = FindObjectOfType<BlackjackGame>();
        if (ui     == null) ui     = FindObjectOfType<BlackjackUI>();
        if (dealer == null) dealer = FindObjectOfType<BlackjackDealer>();

        if (game == null) { Debug.LogError("[Bootstrapper] BlackjackGame not found!"); return; }
        if (ui   == null) { Debug.LogError("[Bootstrapper] BlackjackUI not found!");   return; }

        // Wire dealer reference so game can trigger animations
        if (game.dealer == null && dealer != null) game.dealer = dealer;
        if (game.ui     == null && ui     != null) game.ui     = ui;

        // Wire hole-card reveal: dealer needs to know when to flip sprite
        game.OnCardDealt += (card, isPlayer) =>
        {
            if (!isPlayer && !card.IsFaceUp) return; // skip initial face-down event
            // When a face-down card becomes visible the game fires OnCardDealt again
            // with IsFaceUp = true; dealer.RevealHoleCard handles the sprite swap.
            if (!isPlayer && card.IsFaceUp && dealer != null)
                dealer.RevealHoleCard(card);
        };

        ui.Init(game);
    }
}
