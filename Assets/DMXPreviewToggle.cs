using UnityEngine;
using UnityEngine.InputSystem;

public class DMXPreviewToggle : MonoBehaviour
{
    public MeshRenderer previewMesh;
    public Canvas mainUI;
    public Canvas graphyUI;

    public bool dmxPreview {
        get => _dmxPreview;
    }

    private bool _dmxPreview;

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

    void Update()
    {
        if (Keyboard.current[Key.F11].wasPressedThisFrame)
            EnableDMXPreview(!_dmxPreview);
    }
}
