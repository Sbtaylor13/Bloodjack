using UnityEngine;

/// <summary>
/// Attach to the desk/table object in the room scene.
/// Player walks up and presses E to open the blackjack UI overlay.
/// No scene loading — everything lives in the same scene.
/// </summary>
public class DeskInteractable : MonoBehaviour
{
    [Header("Blackjack UI")]
    [Tooltip("The root Canvas GameObject for the blackjack UI — disabled at start")]
    public GameObject blackjackCanvasRoot;
    public BlackjackGame inlineGame;
    public BlackjackUI inlineUI;

    [Header("Interaction")]
    public float interactRadius = 2f;
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactPromptUI;

    [Header("HUD to hide during blackjack")]
    public GameObject playerHUD;

    public static System.Action OnLeaveTable;

    private Transform _player;
    private bool _isOpen = false;
    private bool _initialized = false;

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;

        // Make sure blackjack UI starts hidden
        blackjackCanvasRoot?.SetActive(false);
        interactPromptUI?.SetActive(false);
    }

    void OnEnable()  => OnLeaveTable += CloseBlackjack;
    void OnDisable() => OnLeaveTable -= CloseBlackjack;

    void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        bool inRange = dist <= interactRadius;

        // Only show prompt when UI is closed and player is in range
        interactPromptUI?.SetActive(inRange && !_isOpen);

        if (inRange && !_isOpen && Input.GetKeyDown(interactKey))
            OpenBlackjack();
    }

    void OpenBlackjack()
    {
        _isOpen = true;
        interactPromptUI?.SetActive(false);
        playerHUD?.SetActive(false);

        blackjackCanvasRoot?.SetActive(true);

        if (!_initialized && inlineGame != null && inlineUI != null)
        {
            inlineGame.SyncChips(); // read collected chips before Init
            inlineUI.Init(inlineGame);
            _initialized = true;
        }
        else if (_initialized && inlineUI != null)
        {
            inlineUI.OnReenter(); // already calls SyncChips internally
        }
    }

    public void CloseBlackjack()
    {
        _isOpen = false;

        // Removed: Cursor.lockState and Cursor.visible lines

        blackjackCanvasRoot?.SetActive(false);
        playerHUD?.SetActive(true);
        interactPromptUI?.SetActive(false);
        inlineGame?.dealer?.ClearTable();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
