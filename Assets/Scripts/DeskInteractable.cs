using System.Collections;
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

    [Header("Room Objects to Hide During Blackjack")]
    public GameObject playerObject;        // your player GameObject
    public GameObject playerHUD;           // any room UI canvas, if you have one

    [Header("Interaction")]
    [Tooltip("How close the player must be (units) to trigger the prompt")]
    public float interactRadius = 2f;
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactPromptUI;   // optional "Press E" label

    private Transform _player;
    private bool _subsceneLoaded = false;

    [Header("Room Objects to Hide During Blackjack")]
    public MonoBehaviour playerController;

    public static System.Action OnLeaveTable;

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;
        interactPromptUI?.SetActive(false);
    }
    void OnEnable()
    {
        OnLeaveTable += CloseBlackjack;
    }

    void OnDisable()
    {
        OnLeaveTable -= CloseBlackjack;
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
        if (playerObject != null) playerObject.SetActive(false);
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        if (playerHUD != null) playerHUD.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!_subsceneLoaded)
        {
            StartCoroutine(LoadBlackjackScene());
        }
        if (playerController != null) playerController.enabled = false;
    }

    IEnumerator LoadBlackjackScene()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(blackjackSceneName, LoadSceneMode.Additive);
        yield return new WaitUntil(() => load.isDone);
        _subsceneLoaded = true;
    }

    public void CloseBlackjack()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerObject != null) playerObject.SetActive(true);
        if (playerHUD != null) playerHUD.SetActive(true);
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        if (playerController != null) playerController.enabled = true;
        StartCoroutine(UnloadBlackjackScene());
    }

    IEnumerator UnloadBlackjackScene()
    {
        AsyncOperation unload = SceneManager.UnloadSceneAsync(blackjackSceneName);
        yield return new WaitUntil(() => unload.isDone);
        _subsceneLoaded = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
