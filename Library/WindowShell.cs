using System.Numerics;
using Dalamud.Bindings.ImGui;
namespace AetherglassUI;

public sealed record Destination(string Id,string Label,string Glyph,Action Draw)
{
    public Action<Vector2,float,uint>? DrawIcon { get; init; }
}
public sealed class WindowShell
{
    public Action? Close {get;set;}
    public Action? Dock {get;set;}
    public string? Selected { get; set; }
    public void Draw(string title,string context,IReadOnlyList<Destination> pages,Action<Vector2,float> mark, Action? footer=null,Func<StateTone>? tone=null)
    {
        if (pages.Count==0) return;
        float scale=ImGui.GetTextLineHeight()/16f;
        var p=ImGui.GetWindowPos();
        ImGui.GetWindowDrawList().AddRectFilledMultiColor(p,p+ImGui.GetWindowSize(),0xff3f2d16,0xff3e292a,0xff3d2635,0xff3e2b21);
        var size=ImGui.GetWindowSize();
        var dl=ImGui.GetWindowDrawList();
        float header=50*scale, railWidth=49*scale, foot=footer==null?0:34*scale;
        float dock=Dock==null?0:42*scale;
        float bodyHeight=Math.Max(1,size.Y-header-foot-dock);
        dl.AddRectFilled(p,p+new Vector2(size.X,header),0x80170f0c);
        dl.AddRectFilled(p+new Vector2(0,header),p+new Vector2(railWidth,size.Y-foot),0x3b140d0a);
        dl.AddLine(p+new Vector2(0,header),p+new Vector2(size.X,header),0x21e8d3c9);
        dl.AddLine(p+new Vector2(railWidth,header),p+new Vector2(railWidth,size.Y-foot),0x21e8d3c9);
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
                    try { if(ImGui.Button((page.DrawIcon==null?page.Glyph:"")+"###"+page.Id,new Vector2(41,40)*scale))active=page; }
                    finally { ImGui.PopStyleColor(); ImGui.PopStyleVar(); }
                    if(selected)ImGui.GetWindowDrawList().AddRectFilled(pos,pos+new Vector2(3,40)*scale,0xffced462);
                    page.DrawIcon?.Invoke(pos+new Vector2(12,11)*scale,18*scale,selected?0xffced462:0xffc3b1a4);
                    if(ImGui.IsItemHovered()||ImGui.IsItemFocused())ImGui.SetTooltip(page.Label);
                }
            }
            finally { ImGui.EndChild(); }
            Selected=active.Id;
            float pad=(size.X<410*scale?9:18)*scale;
            ImGui.SetCursorScreenPos(p+new Vector2(railWidth,header+(size.X<=410*scale?12:16)*scale));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding,new Vector2(pad,0));
            bool body=ImGui.BeginChild("Workspace###"+active.Id,new Vector2(Math.Max(1,size.X-railWidth),Math.Max(1,bodyHeight-20*scale)),false,ImGuiWindowFlags.AlwaysUseWindowPadding);
            ImGui.PopStyleVar();
            try { if(body){ImGui.SetWindowFontScale(1.2f); ImGui.TextUnformatted(active.Label); ImGui.SetWindowFontScale(1); ImGui.Dummy(new Vector2(0,8*scale));active.Draw();} }
            finally { ImGui.EndChild(); }
            if(Dock!=null)
            {
                var origin=p+new Vector2(0,size.Y-foot-dock);
                dl.AddRectFilled(origin,origin+new Vector2(size.X,dock),0x80170f0c);
                dl.AddLine(origin,origin+new Vector2(size.X,0),0x21e8d3c9);
                ImGui.SetCursorScreenPos(origin+new Vector2(14,5)*scale);
                Dock();
            }
            if(footer!=null)
            {
                var start=p+new Vector2(0,size.Y-foot);
                dl.AddRectFilled(start,p+size,0x80170f0c);
                dl.AddLine(start,start+new Vector2(size.X,0),0x21e8d3c9);
                var state=tone?.Invoke()??StateTone.Ready;
                if(state!=StateTone.Ready)
                    dl.AddText(start+new Vector2(17,8)*scale,state==StateTone.Error?0xff8888ff:0xff60bef4,state==StateTone.Pending?"*":"!");
                else {
                dl.AddLine(start+new Vector2(15,17)*scale,start+new Vector2(19,21)*scale,0xffced462,1.5f*scale);
                dl.AddLine(start+new Vector2(19,21)*scale,start+new Vector2(26,13)*scale,0xffced462,1.5f*scale);
                }
                ImGui.SetCursorScreenPos(start+new Vector2(36,8)*scale);footer();
            }
        }
        finally { ImGui.PopStyleColor(6); ImGui.PopStyleVar(3); }
    }
}
