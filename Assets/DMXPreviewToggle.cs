using UnityEngine;
using UnityEngine.InputSystem;

public class DMXPreviewToggle : MonoBehaviour
{
    public InputAction fullscreenAction;
    public MeshRenderer previewMesh;
    public Canvas mainUI;
    public Canvas graphyUI;

    public bool dmxPreview {
        get => _dmxPreview;
    }

    private bool _dmxPreview;
    private int _windowResolutionWidth;
    private int _windowResolutionHeight;

    public void EnableDMXPreview(bool enablePreview)
    {
        if (enablePreview == _dmxPreview)
            return;

        _dmxPreview = enablePreview;

        previewMesh.enabled = _dmxPreview;
        mainUI.enabled = !_dmxPreview;

        if (graphyUI != null)
            graphyUI.enabled = !_dmxPreview;
    }

    public void ToggleDMXPreview(InputAction.CallbackContext ctx)
    {
        EnableDMXPreview(!_dmxPreview);

        if (_dmxPreview)
        {
            _windowResolutionWidth = Screen.width;
            _windowResolutionHeight = Screen.height;

            Screen.SetResolution(Screen.mainWindowDisplayInfo.width, Screen.mainWindowDisplayInfo.height, _dmxPreview);
        } else Screen.SetResolution(_windowResolutionWidth, _windowResolutionHeight, _dmxPreview);
    }

    void Awake()
    {
        fullscreenAction.performed += ToggleDMXPreview;
    }

    void OnEnable()
    {
        fullscreenAction.Enable();
    }

    void OnDisable()
    {
        fullscreenAction.Disable();
    }
}
