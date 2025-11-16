using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class CustomCursor : MonoBehaviour
{
    [FoldoutGroup("References")]
    [SerializeField] private Image _cursorImage;

    [FoldoutGroup("References")]
    [SerializeField] private RectTransform _cursorTransform;

    [FoldoutGroup("Cursor Sprites")]
    [SerializeField] private Sprite _normalSprite;

    [FoldoutGroup("Cursor Sprites")]
    [SerializeField] private Sprite _pressedSprite;

    private bool _isPressed;

    private void Start()
    {
        InitializeCursor();
    }

    private void Update()
    {
        // Sorry, we need Update here for mouse tracking!
        CheckMouseBounds();
        UpdateCursorPosition();
        HandleMouseInput();
    }

    private void InitializeCursor()
    {
        Cursor.visible = false;

        if (_cursorImage == null)
            _cursorImage = GetComponent<Image>();

        if (_cursorTransform == null)
            _cursorTransform = GetComponent<RectTransform>();

        SetCursorSprite(_normalSprite);
    }

    private void CheckMouseBounds()
    {
        bool isWithinBounds = IsMouseWithinScreenBounds();

        Cursor.visible = !isWithinBounds;

        if (_cursorImage != null)
            _cursorImage.enabled = isWithinBounds;
    }

    private bool IsMouseWithinScreenBounds()
    {
        Vector3 mousePos = Input.mousePosition;

        return mousePos.x >= 0 && mousePos.x <= Screen.width &&
               mousePos.y >= 0 && mousePos.y <= Screen.height;
    }

    private void UpdateCursorPosition()
    {
        if (_cursorTransform == null) return;

        _cursorTransform.position = Input.mousePosition;
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            OnMousePressed();
        }
        else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
        {
            OnMouseReleased();
        }
    }

    private void OnMousePressed()
    {
        _isPressed = true;
        SetCursorSprite(_pressedSprite);
    }

    private void OnMouseReleased()
    {
        _isPressed = false;
        SetCursorSprite(_normalSprite);
    }

    private void SetCursorSprite(Sprite sprite)
    {
        if (_cursorImage != null && sprite != null)
        {
            _cursorImage.sprite = sprite;
        }
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
    }
}