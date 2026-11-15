using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;

namespace AetherglassUI;

public sealed class PreviewCameraInput
{
    private nint dragWindow;
    private Point dragOrigin;
    private ImGuiMouseButton dragButton;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    public void Read(float scale, ref Vector2 rotate, ref Vector2 pan)
    {
        if (ImGui.IsItemActivated() && GetCursorPos(out dragOrigin))
        {
            dragWindow = GetForegroundWindow();
            dragButton = ImGui.IsMouseClicked(ImGuiMouseButton.Right) ? ImGuiMouseButton.Right : ImGuiMouseButton.Left;
        }
        if (dragWindow == 0) return;
        if (GetForegroundWindow() != dragWindow || !ImGui.IsItemActive() || !ImGui.IsMouseDown(dragButton))
        {
            EndDrag();
            return;
        }
        ImGui.SetMouseCursor(ImGuiMouseCursor.None);
        if (!GetCursorPos(out var cursor) || cursor == dragOrigin) return;
        var delta = new Vector2(cursor.X - dragOrigin.X, cursor.Y - dragOrigin.Y) / scale;
        if (dragButton == ImGuiMouseButton.Left) rotate += delta;
        else pan += new Vector2(delta.X, -delta.Y);
        SetCursorPos(dragOrigin.X, dragOrigin.Y);
    }

    public void EndDrag()
    {
        if (dragWindow != 0 && GetForegroundWindow() == dragWindow)
            SetCursorPos(dragOrigin.X, dragOrigin.Y);
        dragWindow = 0;
    }

}
