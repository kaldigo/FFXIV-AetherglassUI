using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AetherglassUI;

public readonly record struct Pane(string Id, float Weight, Action Draw, bool FixedWidth=false, bool EdgeToEdge=false);
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
        var inset=ImGui.GetStyle().CellPadding;
        bool flush=panes.Any(p=>p.EdgeToEdge);
        if(flush)ImGui.PushStyleVar(ImGuiStyleVar.CellPadding,Vector2.Zero);
        bool visible=ImGui.BeginTable(id,panes.Length,ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerV);
        if(flush)ImGui.PopStyleVar();
        if(!visible)return;
        try
        {
            foreach (var pane in panes) ImGui.TableSetupColumn(pane.Id, pane.FixedWidth?ImGuiTableColumnFlags.WidthFixed:ImGuiTableColumnFlags.WidthStretch, pane.FixedWidth?pane.Weight*Scale:pane.Weight);
            ImGui.TableNextRow();
            foreach (var pane in panes)
            {
                ImGui.TableNextColumn();
                var padding=pane.EdgeToEdge?Vector2.Zero:new Vector2(10,4)*Scale+(flush?inset:Vector2.Zero);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding,padding);
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
        dl.AddRectFilled(p,p+size, selected ? 0xb4474530u : ImGui.IsItemHovered() ? 0xba514941u : 0x80352d27u,5*s);
        dl.AddRect(p,p+size,selected ? Accent : Border,5*s);
        var a=p+new Vector2(6*s); var b=a+new Vector2(40*s);
        if (icon.HasValue) icon.Value.Paint(a,b);
        if (missing) dl.AddRect(a,b,Missing,4*s);
        var x=icon.HasValue || missing ? b.X+9*s : a.X+4*s;
        dl.PushClipRect(new Vector2(x,p.Y),p+size-new Vector2((selected?25:3)*s,0),true);
        dl.AddText(new Vector2(x,p.Y+(detail.Length==0?16:6)*s),0xffefefef,label);
        if (detail.Length>0) dl.AddText(new Vector2(x,p.Y+27*s),missing?Missing:0xffbfb5ac,detail);
        dl.PopClipRect();
        if(selected)
        {
            var check=p+new Vector2(size.X-18*s,size.Y*.52f);
            dl.AddLine(check-new Vector2(5,1)*s,check+new Vector2(-1,3)*s,Accent,2*s);
            dl.AddLine(check+new Vector2(-1,3)*s,check+new Vector2(6,-5)*s,Accent,2*s);
        }
        return clicked;
    }

    public static bool Swatch(string id,uint rgba,string label,bool selected=false,float side=26,bool round=false,bool spectrum=false,float width=0)
    {
        var p=ImGui.GetCursorScreenPos(); var size=new Vector2(width>0?width:side,side)*Scale;
        bool clicked=ImGui.InvisibleButton(id,size);
        var dl=ImGui.GetWindowDrawList();
        float radius=round?size.X/2:4*Scale;
        dl.AddRectFilled(p,p+size,rgba,radius);
        if(spectrum)
        {
            var center=p+size/2;
            for(int i=0;i<48;i++)
            {
                float a=i*MathF.Tau/48,b=(i+1)*MathF.Tau/48;
                float r=0,g=0,blue=0; ImGui.ColorConvertHSVtoRGB(i/48f,.8f,.95f,ref r,ref g,ref blue);
                dl.AddTriangleFilled(center,center+new Vector2(MathF.Cos(a),MathF.Sin(a))*radius,
                    center+new Vector2(MathF.Cos(b),MathF.Sin(b))*radius,ImGui.GetColorU32(new Vector4(r,g,blue,1)));
            }
        }
        dl.AddRect(p,p+size,selected?Accent:ImGui.IsItemHovered()?0xffede8e2u:Border,radius,ImDrawFlags.None,selected?2:1);
        if(round && selected)dl.AddCircle(p+size/2,radius-3*Scale,0xc025211du,32,2*Scale);
        if(selected && !round)
        {
            var ink=((rgba&255)*299+((rgba>>8)&255)*587+((rgba>>16)&255)*114)>145000?0xff202022u:0xfffafafau;
            dl.AddLine(p+size*.28f,p+new Vector2(size.X*.44f,size.Y*.62f),ink,2*Scale);
            dl.AddLine(p+new Vector2(size.X*.44f,size.Y*.62f),p+new Vector2(size.X*.75f,size.Y*.32f),ink,2*Scale);
        }
        if(ImGui.IsItemHovered()) ImGui.SetTooltip(label);
        return clicked;
    }

    public static bool RoundAction(string label,bool active,Action<ImDrawListPtr,Vector2,float,uint> glyph,uint? tint=null,string? tooltip=null)
    {
        var side=30*Scale;
        var p=ImGui.GetCursorScreenPos();
        bool clicked=ImGui.InvisibleButton(label,new Vector2(side));
        var center=p+new Vector2(side/2);
        var dl=ImGui.GetWindowDrawList();
        dl.AddCircleFilled(center,side*.46f,tint.HasValue?(0xff000000u|((tint.Value>>2)&0x003f3f3fu)):active?0xff484737u:0xff302c28u);
        dl.AddCircle(center,side*.46f,tint??(ImGui.IsItemHovered()?0xffe7dfbeu:active?0xff90bad2u:0xff776c66u),32,1.6f*Scale);
        glyph(dl,center,side,tint??(active?0xfff4eee3u:0xffc2b9afu));
        if(ImGui.IsItemHovered())ImGui.SetTooltip(tooltip??label);
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
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding,new Vector2(14,12)*Scale);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding,9*Scale);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize,1*Scale);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing,new Vector2(6,6)*Scale);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,new Vector2(8,5)*Scale);
        ImGui.PushStyleColor(ImGuiCol.PopupBg,new Vector4(.09f,.11f,.14f,.99f));
        ImGui.PushStyleColor(ImGuiCol.Border,new Vector4(.40f,.47f,.52f,.8f));
        if(ImGui.BeginPopup(id))return true;
        PopPickerStyle();
        return false;
    }
    private static void PopPickerStyle()
    {
        ImGui.PopStyleColor(2);ImGui.PopStyleVar(5);
    }
    public static void EndPicker()
    {
        ImGui.EndPopup();PopPickerStyle();
    }
    public static void PickerHeading(string title)
    {
        var right=ImGui.GetCursorPosX()+ImGui.GetContentRegionAvail().X;
        ImGui.AlignTextToFramePadding();ImGui.TextUnformatted(title);
        ImGui.SameLine();ImGui.SetCursorPosX(right-24*Scale);
        if(ImGui.Button("\u00d7##close",new Vector2(24*Scale)))ImGui.CloseCurrentPopup();
        ImGui.Separator();
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

    // Preserve proportions while filling the pane. Report the uncropped bounds for backdrop sampling.
    public static void FillViewport(string id,Picture picture,float aspect,Action<Vector2,Vector2> input)
    {
        var size=ImGui.GetContentRegionAvail();
        if(size.X<1 || size.Y<1 || aspect<=0)return;
        var full=new Vector2(Math.Max(size.X,size.Y*aspect),Math.Max(size.Y,size.X/aspect));
        var inset=(full-size)/2;
        var uv=(picture.Uv1-picture.Uv0)*inset/full;
        var origin=ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton(id,size,ImGuiButtonFlags.MouseButtonLeft|ImGuiButtonFlags.MouseButtonRight);
        new Picture(picture.Texture,picture.Uv0+uv,picture.Uv1-uv).Paint(origin,origin+size);
        input(origin-inset,origin+size+inset);
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

