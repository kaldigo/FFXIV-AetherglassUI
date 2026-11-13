using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AetherglassUI;

public static class ScrollRegion
{
    // Extend the scroll surface to the parent edge. Keep its original content inset.
    public static bool Begin(string id)
    {
        float inset=Math.Max(0,ImGui.GetCursorScreenPos().X-ImGui.GetWindowPos().X);
        float width=ImGui.GetContentRegionAvail().X+inset;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX()-inset);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding,new Vector2(inset,0));
        var list=ImGui.GetWindowDrawList();
        var min=list.GetClipRectMin();
        var max=list.GetClipRectMax();
        var origin=ImGui.GetWindowPos();
        ImGui.PushClipRect(new Vector2(origin.X,min.Y),new Vector2(origin.X+ImGui.GetWindowWidth(),max.Y),false);
        try{return ImGui.BeginChild(id,new Vector2(width+inset,0),false,ImGuiWindowFlags.AlwaysUseWindowPadding);}
        finally{ImGui.PopStyleVar();}
    }
    public static void End(){ImGui.EndChild();ImGui.PopClipRect();}
}
