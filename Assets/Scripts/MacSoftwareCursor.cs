#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
using UnityEngine;

internal sealed class MacSoftwareCursor : MonoBehaviour
{
    // The cursor artwork fills about 76% of the texture, so 84 gives it about 64 visible pixels.
    private const int Size = 84;
    private Texture2D texture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        var texture = Resources.Load<Texture2D>("Curosr");
        Debug.Assert(texture != null, "Missing macOS cursor texture.");
        if (texture == null) return;

        var cursor = new GameObject(nameof(MacSoftwareCursor)).AddComponent<MacSoftwareCursor>();
        cursor.texture = texture;
        DontDestroyOnLoad(cursor.gameObject);
        Cursor.visible = false;
    }

    private void Update()
    {
        // The macOS menu bar can restore the system arrow while the game still has focus.
        Cursor.visible = false;
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || !Application.isFocused) return;

        var mouse = Input.mousePosition;
        if (mouse.x < 0 || mouse.x >= Screen.width || mouse.y < 0 || mouse.y >= Screen.height) return;

        GUI.depth = int.MinValue;
        GUI.DrawTexture(new Rect(mouse.x, Screen.height - mouse.y, Size, Size), texture);
    }
}
#endif
