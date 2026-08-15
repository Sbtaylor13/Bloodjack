using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Replaces mouse clicking with center-screen raycast + E to confirm.
/// Attach to the same GameObject as BlackjackUI.
/// Highlights the button the crosshair is over and pressing E clicks it.
/// </summary>
public class CrosshairUISelector : MonoBehaviour
{
    [Header("References")]
    public Camera uiCamera;           // your main camera
    public GraphicRaycaster raycaster; // on the Canvas

    [Header("Highlight")]
    [Tooltip("Color applied to the button image when hovered")]
    public Color highlightColor = new Color(1f, 0.85f, 0f, 1f); // gold

    public KeyCode confirmKey = KeyCode.E;

    private Button _hoveredButton;
    private Dictionary<Button, Color> _originalColors = new();

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        CheckCrosshairHover();

        if (_hoveredButton != null && Input.GetKeyDown(confirmKey))
            ClickButton(_hoveredButton);
    }

    void CheckCrosshairHover()
    {
        // Cast from screen center
        PointerEventData ped = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Screen.width / 2f, Screen.height / 2f)
        };

        List<RaycastResult> results = new();
        raycaster.Raycast(ped, results);

        Button found = null;
        foreach (var result in results)
        {
            var btn = result.gameObject.GetComponentInParent<Button>();
            if (btn != null && btn.interactable)
            {
                found = btn;
                break;
            }
        }

        if (found != _hoveredButton)
        {
            // Unhighlight old
            if (_hoveredButton != null)
                RestoreColor(_hoveredButton);

            // Highlight new
            _hoveredButton = found;
            if (_hoveredButton != null)
                ApplyHighlight(_hoveredButton);
        }
    }

    void ApplyHighlight(Button btn)
    {
        var img = btn.GetComponent<Image>();
        if (img == null) return;
        if (!_originalColors.ContainsKey(btn))
            _originalColors[btn] = img.color;
        img.color = highlightColor;
    }

    void RestoreColor(Button btn)
    {
        var img = btn.GetComponent<Image>();
        if (img == null) return;
        if (_originalColors.TryGetValue(btn, out Color original))
            img.color = original;
    }

    void ClickButton(Button btn)
    {
        btn.onClick.Invoke();
    }

    void OnDisable()
    {
        // Clean up highlight when UI closes
        if (_hoveredButton != null)
        {
            RestoreColor(_hoveredButton);
            _hoveredButton = null;
        }
    }
}