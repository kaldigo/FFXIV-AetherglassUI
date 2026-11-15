using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AetherglassUI;

public readonly record struct Pane(string Id, float Weight, Action Draw);
public readonly record struct Picture(ImTextureID Texture, Vector2 Uv0, Vector2 Uv1)
{
    public Picture(ImTextureID texture) : this(texture, Vector2.Zero, Vector2.One) { }
    public void Paint(Vector2 min, Vector2 max) =>
        ImGui.GetWindowDrawList().AddImage(Texture, min, max, Uv0, Uv1);
}

public static class WorkspaceControls
{
    public static float Scale => ImGui.GetTextLineHeight() / 16f;
    public const uint Accent = 0xffcbd768;
    public const uint Border = 0x506f808b;
    public const uint Missing = 0xff817ef0;
    public static void Split(string id, params Pane[] panes)
    {
        if (!ImGui.BeginTable(id, panes.Length, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersInnerV)) return;
        try
        {
            foreach (var pane in panes) ImGui.TableSetupColumn(pane.Id, ImGuiTableColumnFlags.WidthStretch, pane.Weight);
            ImGui.TableNextRow();
            foreach (var pane in panes)
            {
                ImGui.TableNextColumn();
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 4) * Scale);
                bool show = ImGui.BeginChild(pane.Id, Vector2.Zero, false, ImGuiWindowFlags.AlwaysUseWindowPadding);
                ImGui.PopStyleVar();
                try { if (show) pane.Draw(); }
                finally { ImGui.EndChild(); }
            }
        }
        finally { ImGui.EndTable(); }
    }

    public static bool Tile(string id, string label, string detail, Picture? icon, bool selected, bool missing, float width)
    {
        float s = Scale;
        var p = ImGui.GetCursorScreenPos();
        var size = new Vector2(Math.Max(1,width), 52*s);
        bool clicked = ImGui.InvisibleButton(id,size);
        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(p,p+size, selected ? 0x903d5540u : ImGui.IsItemHovered() ? 0xba514941u : 0x80352d27u,5*s);
        dl.AddRect(p,p+size,selected ? Accent : Border,5*s);
        var a=p+new Vector2(6*s); var b=a+new Vector2(40*s);
        if (icon.HasValue) icon.Value.Paint(a,b);
        if (missing) dl.AddRect(a,b,Missing,4*s);
        var x=icon.HasValue || missing ? b.X+9*s : a.X+4*s;
        dl.PushClipRect(new Vector2(x,p.Y),p+size-new Vector2(3*s,0),true);
        dl.AddText(new Vector2(x,p.Y+(detail.Length==0?16:6)*s),0xffefefef,label);
        if (detail.Length>0) dl.AddText(new Vector2(x,p.Y+27*s),missing?Missing:0xffbfb5ac,detail);
        dl.PopClipRect();
        return clicked;
    }

    public static bool Swatch(string id,uint rgba,string label,bool selected=false,float side=26,bool round=false)
    {
        var p=ImGui.GetCursorScreenPos(); var size=new Vector2(side*Scale);
        bool clicked=ImGui.InvisibleButton(id,size);
        var dl=ImGui.GetWindowDrawList();
        float radius=round?size.X/2:4*Scale;
        dl.AddRectFilled(p,p+size,rgba,radius);
        dl.AddRect(p,p+size,selected?Accent:Border,radius,ImDrawFlags.None,selected?3:1);
        if(ImGui.IsItemHovered()) ImGui.SetTooltip(label);
        return clicked;
    }

    public static bool BeginPicker(string id,float width,float preferredHeight)
    {
        if(!ImGui.IsPopupOpen(id))return false;
        var bounds=ImGui.GetWindowPos();
        var right=bounds.X+ImGui.GetWindowSize().X;
        var vp=ImGui.GetWindowViewport();
        width=Math.Min(width*Scale,vp.WorkSize.X);
        float left=Math.Clamp(ImGui.GetItemRectMin().X,vp.WorkPos.X,Math.Max(vp.WorkPos.X,right-width));
        float bottom=vp.WorkPos.Y+vp.WorkSize.Y;
        float top=ImGui.GetItemRectMax().Y+3*Scale;
        if(top+preferredHeight*Scale>bottom) top=Math.Max(vp.WorkPos.Y,bottom-preferredHeight*Scale);
        ImGui.SetNextWindowPos(new(left,top));
        ImGui.SetNextWindowSizeConstraints(new(width,0),new(width,vp.WorkSize.Y));
        return ImGui.BeginPopup(id);
    }

    public static bool Thumbnail(string id,Picture? image,string label,bool selected,float side)
    {
        var p=ImGui.GetCursorScreenPos(); var size=new Vector2(side*Scale);
        bool clicked=ImGui.InvisibleButton(id,size);
        if(image.HasValue) image.Value.Paint(p+Vector2.One,p+size-Vector2.One);
        else ImGui.GetWindowDrawList().AddText(p+new Vector2(5*Scale),0xffeee5df,label);
        ImGui.GetWindowDrawList().AddRect(p,p+size,selected?Accent:Border,4,ImDrawFlags.None,selected?2:1);
        if(ImGui.IsItemHovered()) ImGui.SetTooltip(label);
        return clicked;
    }

    public static void Viewport(string id,Picture picture,float aspect,Action input,float reservedHeight=0)
    {
        var space=ImGui.GetContentRegionAvail();
        float width=Math.Min(space.X,Math.Max(1,space.Y-reservedHeight)*aspect);
        if(width<1 || aspect<=0) return;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX()+(space.X-width)/2);
        ImGui.InvisibleButton(id,new Vector2(width,width/aspect),ImGuiButtonFlags.MouseButtonLeft|ImGuiButtonFlags.MouseButtonRight);
        picture.Paint(ImGui.GetItemRectMin(),ImGui.GetItemRectMax());
        input();
    }
}

