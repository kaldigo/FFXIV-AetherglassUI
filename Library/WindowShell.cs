using System.Numerics;
using Dalamud.Bindings.ImGui;
namespace AetherglassUI;

public sealed record Destination(string Id,string Label,string Glyph,Action Draw)
{
    public Action? OnBack { get; init; }
    public Action<Vector2,float,uint>? DrawIcon { get; init; }
}
public sealed class WindowShell
{
    public const ImGuiWindowFlags HostFlags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    public static void PushHostStyle()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
    }

    public static void PopHostStyle() => ImGui.PopStyleVar(2);

    private static void DrawBackdrop(ImDrawListPtr draw, Vector2 origin, Vector2 size, float radius)
    {
        int first = draw.VtxBuffer.Size;
        draw.AddRectFilled(origin, origin + size, 0xffffffff, radius);
        // Tint the rounded mesh while retaining its antialiased edge coverage.
        var vertices = draw.VtxBuffer;
        for (int i = first; i < vertices.Size; i++)
        {
            var vertex = vertices[i];
            var uv = Vector2.Clamp((vertex.Pos - origin) / size, Vector2.Zero, Vector2.One);
            var top = Vector3.Lerp(new(22,45,63), new(42,41,62), uv.X);
            var bottom = Vector3.Lerp(new(33,43,62), new(53,38,61), uv.X);
            var rgb = Vector3.Lerp(top, bottom, uv.Y);
            vertex.Col = (vertex.Col & 0xff000000) | (uint)rgb.X | ((uint)rgb.Y << 8) | ((uint)rgb.Z << 16);
            vertices[i] = vertex;
        }
    }

    public Action? Close {get;set;}
    public Action? Dock {get;set;}
    private readonly List<Destination> subpages = [];
    private string? selected;

    public string? Selected
    {
        get => selected;
        set
        {
            if (selected == value) return;
            selected = value;
            subpages.Clear();
        }
    }

    public void OpenSubpage(Destination page)
    {
        int existing = subpages.FindIndex(p => p.Id == page.Id);
        if (existing >= 0) subpages.RemoveRange(existing, subpages.Count - existing);
        subpages.Add(page);
    }

    public bool Back()
    {
        if (subpages.Count == 0) return false;
        var leaving = subpages[^1];
        subpages.RemoveAt(subpages.Count - 1);
        leaving.OnBack?.Invoke();
        return true;
    }
    public void Draw(string title,string context,IReadOnlyList<Destination> pages,Action<Vector2,float> mark, Action? footer=null,Func<StateTone>? tone=null)
    {
        if (pages.Count==0) return;
        float scale=ImGui.GetTextLineHeight()/16f;
        var p=ImGui.GetWindowPos();

        var size=ImGui.GetWindowSize();
        var dl=ImGui.GetWindowDrawList();
        float header=50*scale, railWidth=49*scale, foot=footer==null?0:34*scale;
        float dock=Dock==null?0:42*scale;
        float bodyHeight=Math.Max(1,size.Y-header-foot-dock);
        float radius = 9 * scale;
        dl.PushClipRect(p, p + size, false);
        try
        {
            DrawBackdrop(dl,p,size,radius);
            dl.AddRectFilled(p,p+new Vector2(size.X,header),0x80170f0c,radius,ImDrawFlags.RoundCornersTop);
            dl.AddRectFilled(p+new Vector2(0,header),p+new Vector2(railWidth,size.Y-foot),0x3b140d0a,radius,footer==null && Dock==null ? ImDrawFlags.RoundCornersBottomLeft : ImDrawFlags.RoundCornersNone);
            dl.AddLine(p+new Vector2(0,header),p+new Vector2(size.X,header),0x21e8d3c9);
            dl.AddLine(p+new Vector2(railWidth,header),p+new Vector2(railWidth,size.Y-foot),0x21e8d3c9);
        } finally { dl.PopClipRect(); }
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,5*scale);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize,1*scale);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,new Vector2(10,7.5f)*scale);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled,new Vector4(.68f,.72f,.78f,1));
        ImGui.PushStyleColor(ImGuiCol.Button,new Vector4(.08f,.10f,.14f,.50f));
        ImGui.PushStyleColor(ImGuiCol.FrameBg,new Vector4(.07f,.09f,.13f,.48f));
        ImGui.PushStyleColor(ImGuiCol.Border,new Vector4(.72f,.77f,.85f,.24f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered,new Vector4(.16f,.34f,.35f,.85f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,new Vector4(.14f,.44f,.42f,1));
        try
        {
            mark(p+new Vector2(15,10)*scale,26*scale);
            var text=p+new Vector2(51,14)*scale;
            dl.AddText(text,0xffeeebe8,title);
            dl.AddText(text+new Vector2(ImGui.CalcTextSize(title).X+12*scale,0),0xffb9ada6,context);
            if(Close!=null)
            {
                ImGui.SetCursorScreenPos(p+new Vector2(size.X-42*scale,10*scale));
                if(ImGui.Button("×###close",new Vector2(30*scale,30*scale)))Close();
                if(ImGui.IsItemHovered())ImGui.SetTooltip("Close window");
            }
            var active=pages.FirstOrDefault(x=>x.Id==Selected)??pages[0];
            ImGui.SetCursorScreenPos(p+new Vector2(3*scale,header+14*scale));
            bool rail=ImGui.BeginChild("AetherglassUI rail",new Vector2(railWidth-4*scale,Math.Max(1,bodyHeight-14*scale)),false);
            try
            {
                if(rail) foreach(var page in pages)
                {
                    var pos=ImGui.GetCursorScreenPos();
                    bool selected=page==active;
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize,0);
                    ImGui.PushStyleColor(ImGuiCol.Button,selected?new Vector4(.42f,.87f,.83f,.12f):Vector4.Zero);
                    try { if(ImGui.Button((page.DrawIcon==null?page.Glyph:"")+"###"+page.Id,new Vector2(41,40)*scale)){active=page;subpages.Clear();} }
                    finally { ImGui.PopStyleColor(); ImGui.PopStyleVar(); }
                    if(selected)ImGui.GetWindowDrawList().AddRectFilled(pos,pos+new Vector2(3,40)*scale,0xffced462);
                    page.DrawIcon?.Invoke(pos+new Vector2(12,11)*scale,18*scale,selected?0xffced462:0xffc3b1a4);
                    if(Controls.HintRequested())ImGui.SetTooltip(page.Label);
                }
            }
            finally { ImGui.EndChild(); }
            Selected=active.Id;
            var current = subpages.Count == 0 ? active : subpages[^1];
            var bodyId = active.Id + string.Concat(subpages.Select(page => "/" + page.Id));
            float pad=(size.X<410*scale?9:18)*scale;
            ImGui.SetCursorScreenPos(p+new Vector2(railWidth,header+(size.X<=410*scale?12:16)*scale));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding,new Vector2(pad,0));
            bool body=ImGui.BeginChild("Workspace###"+bodyId,new Vector2(Math.Max(1,size.X-railWidth),Math.Max(1,bodyHeight-20*scale)),false,ImGuiWindowFlags.AlwaysUseWindowPadding);
            ImGui.PopStyleVar();
            try
            {
                if (body)
                {
                    if (subpages.Count > 0)
                    {
                        if (Controls.ActionButton("Back###subpage-back")) Back();
                        ImGui.TextWrapped(string.Join("  /  ", new[] { active.Label }.Concat(subpages.Select(page => page.Label))));
                    }
                    if (current == (subpages.Count == 0 ? active : subpages[^1]))
                    {
                        ImGui.SetWindowFontScale(1.2f);
                        ImGui.TextUnformatted(current.Label);
                        ImGui.SetWindowFontScale(1);
                        ImGui.Dummy(new Vector2(0,8*scale));
                        current.Draw();
                    }
                }
            }
            finally { ImGui.EndChild(); }
            if(Dock!=null)
            {
                var origin=p+new Vector2(0,size.Y-foot-dock);
                dl.PushClipRect(p,p+size,false);
                dl.AddRectFilled(origin,origin+new Vector2(size.X,dock),0x80170f0c,radius,footer==null?ImDrawFlags.RoundCornersBottom:ImDrawFlags.RoundCornersNone);
                dl.PopClipRect();
                dl.AddLine(origin,origin+new Vector2(size.X,0),0x21e8d3c9);
                ImGui.SetCursorScreenPos(origin+new Vector2(14,5)*scale);
                Dock();
            }
            if(footer!=null)
            {
                var start=p+new Vector2(0,size.Y-foot);
                dl.PushClipRect(p,p+size,false);
                dl.AddRectFilled(start,p+size,0x80170f0c,radius,ImDrawFlags.RoundCornersBottom);
                dl.PopClipRect();
                dl.AddLine(start,start+new Vector2(size.X,0),0x21e8d3c9);
                var state=tone?.Invoke()??StateTone.Ready;
                if(state!=StateTone.Ready)
                    dl.AddText(start+new Vector2(17,8)*scale,state==StateTone.Error?0xff8888ff:0xff60bef4,state==StateTone.Pending?"*":"!");
                else {
                dl.AddLine(start+new Vector2(15,17)*scale,start+new Vector2(19,21)*scale,0xffced462,1.5f*scale);
                dl.AddLine(start+new Vector2(19,21)*scale,start+new Vector2(26,13)*scale,0xffced462,1.5f*scale);
                }
                ImGui.SetCursorScreenPos(start+new Vector2(36,2)*scale);ImGui.AlignTextToFramePadding();footer();
            }
        }
        finally { ImGui.PopStyleColor(6); ImGui.PopStyleVar(3); }
    }
}
