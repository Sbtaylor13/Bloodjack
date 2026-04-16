using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to your desk object in the 3D room.
/// The player walks up and presses Interact → loads the Blackjack subscene additively
/// (or activates a Canvas overlay — whichever you prefer).
/// 
/// ── TWO INTEGRATION MODES ──────────────────────────────────────────────────
/// 
/// MODE A – Additive scene loading (recommended for full 2D subscene):
///   1. Build a separate "BlackjackScene" with your 2D dealer, table, cameras, etc.
///   2. Add it to Build Settings.
///   3. Set useAdditiveScene = true and blackjackSceneName = "BlackjackScene".
///
/// MODE B – In-scene Canvas overlay:
///   1. Build the blackjack UI as a Canvas in the same scene (disabled at start).
///   2. Drag that Canvas root into blackjackCanvasRoot.
///   3. Set useAdditiveScene = false.
/// </summary>
public class DeskInteractable : MonoBehaviour
{
    [Header("Mode")]
    public bool useAdditiveScene = false;

    [Header("Mode A – Additive Scene")]
    public string blackjackSceneName = "BlackjackScene";

    [Header("Mode B – Canvas Overlay")]
    public GameObject blackjackCanvasRoot;
    public BlackjackGame inlineGame;
    public BlackjackUI inlineUI;

    [Header("Interaction")]
    [Tooltip("How close the player must be (units) to trigger the prompt")]
    public float interactRadius = 2f;
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactPromptUI;   // optional "Press E" label

    private Transform _player;
    private bool _subsceneLoaded = false;

    void Start()
    {
        // Find player — tag yours "Player" or assign directly
        var p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;

        interactPromptUI?.SetActive(false);
    }

    void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        bool inRange = dist <= interactRadius;

        interactPromptUI?.SetActive(inRange);

        if (inRange && Input.GetKeyDown(interactKey))
            OpenBlackjack();
    }

    void OpenBlackjack()
    {
        if (useAdditiveScene)
        {
            if (!_subsceneLoaded)
            {
                SceneManager.LoadSceneAsync(blackjackSceneName, LoadSceneMode.Additive);
                _subsceneLoaded = true;
            }
            // Optionally disable player input here (e.g. pause 3D movement)
        }
        else
        {
            if (blackjackCanvasRoot == null)
            { Debug.LogError("[DeskInteractable] blackjackCanvasRoot is not assigned."); return; }

            blackjackCanvasRoot.SetActive(true);

            if (inlineGame != null && inlineUI != null)
                inlineUI.Init(inlineGame);
        }
    }

    /// <summary>Call this from BlackjackUI.leaveTableButton to return to 3D.</summary>
    public void CloseBlackjack()
    {
        if (useAdditiveScene)
        {
            SceneManager.UnloadSceneAsync(blackjackSceneName);
            _subsceneLoaded = false;
        }
        else
        {
            blackjackCanvasRoot?.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
