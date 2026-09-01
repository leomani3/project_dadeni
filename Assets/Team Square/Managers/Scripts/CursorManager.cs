using MyBox;
using UnityEngine;

public class CursorManager : Singleton<CursorManager>
{
    void Start()
    {
        Cursor.SetCursor(Resources.Load<Texture2D>("Textures/mouse_cursor"), new Vector2(0.5f, 0.5f), CursorMode.ForceSoftware);
    }

    public void SetCursorHighlight(bool highlighted)
    {
        if (highlighted)
            Cursor.SetCursor(Resources.Load<Texture2D>("Textures/mouse_cursor_highlight"), new Vector2(0.5f, 0.5f), CursorMode.ForceSoftware);
        else
            Cursor.SetCursor(Resources.Load<Texture2D>("Textures/mouse_cursor"), new Vector2(0.5f, 0.5f), CursorMode.ForceSoftware);
    }
}