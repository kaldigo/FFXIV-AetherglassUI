using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AetherglassUI;

public static class Controls
{
    public static void Row(string id, string label, string detail, Action draw)
    {
        Row(id,label,detail,draw,FieldLayout.Flexible,true,true);
    }

    internal static void Row(string id,string label,string detail,Action draw,FieldLayout layout,bool first,bool last)
    {
        float scale=ImGui.GetTextLineHeight()/16f;
        float width=ImGui.GetContentRegionAvail().X;
        var start=ImGui.GetCursorScreenPos();
        bool stacked=width<=312*scale && layout is not (FieldLayout.Switch or FieldLayout.Color);
        float fieldWidth=layout switch {FieldLayout.Number=>94*scale,FieldLayout.Switch=>40*scale,FieldLayout.Color=>116*scale,_=>Math.Min(200*scale,width*.45f)};
        float pad=14*scale;
        float contentWidth=Math.Max(1,width-2*pad);
        var dl=ImGui.GetWindowDrawList();
        dl.ChannelsSplit(2);
        ImGui.PushID(id);
        try
        {
            dl.ChannelsSetCurrent(1);
            float labelWidth=stacked?contentWidth:Math.Max(1,contentWidth-fieldWidth-12*scale);
            float labelHeight=ImGui.CalcTextSize(label,false,labelWidth).Y;
            float detailHeight=detail.Length==0?0:ImGui.CalcTextSize(detail,false,labelWidth).Y+2*scale;
            float textHeight=labelHeight+detailHeight;
            float controlHeight=layout==FieldLayout.Switch?23*scale:ImGui.GetFrameHeight();
            float innerHeight=Math.Max(textHeight,controlHeight);
            var labelPos=start+new Vector2(pad,10*scale+(stacked?0:(innerHeight-textHeight)/2));
            ImGui.SetCursorScreenPos(labelPos);
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX()+labelWidth);
            ImGui.TextUnformatted(label);
            if(detail.Length>0)
            {
                ImGui.SetCursorScreenPos(labelPos+new Vector2(0,labelHeight+2*scale));
                ImGui.PushStyleColor(ImGuiCol.Text,0xffcfc6b6);
                ImGui.TextUnformatted(detail);
                ImGui.PopStyleColor();
            }
            ImGui.PopTextWrapPos();
            var fieldPos=stacked ? start+new Vector2(pad,10*scale+textHeight+6*scale) :
                start+new Vector2(width-pad-fieldWidth,10*scale+(innerHeight-controlHeight)/2);
            ImGui.SetCursorScreenPos(fieldPos);
            ImGui.PushItemWidth(stacked && layout==FieldLayout.Flexible?contentWidth:fieldWidth);
            ImGui.BeginGroup();
            try { draw(); } finally { ImGui.EndGroup(); ImGui.PopItemWidth(); }
            float bottom=Math.Max(ImGui.GetItemRectMax().Y,labelPos.Y+textHeight)+10*scale;
            var end=new Vector2(start.X+width,Math.Max(start.Y+44*scale,bottom));
            dl.ChannelsSetCurrent(0);
            var corners=(first?ImDrawFlags.RoundCornersTop:0)|(last?ImDrawFlags.RoundCornersBottom:0);
            dl.AddRectFilled(start,end,0x85170f0c,6*scale,corners==0?ImDrawFlags.RoundCornersNone:corners);
            if(!first) dl.AddLine(start+new Vector2(pad,0),start+new Vector2(width-pad,0),0x21e8d3c9);
            ImGui.SetCursorScreenPos(start);
            ImGui.Dummy(end-start);
        }
        finally { ImGui.PopID(); dl.ChannelsMerge(); }
    }

    public static bool Search(string id,string hint,ref string query,int limit=160)
    {
        float scale=ImGui.GetTextLineHeight()/16f;
        var pos=ImGui.GetCursorScreenPos();
        ImGui.SetNextItemWidth(query.Length==0?-1:-ImGui.CalcTextSize("Clear").X-32*scale);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,new Vector2(36,10)*scale);
        bool changed;
        try { changed=ImGui.InputTextWithHint(id,hint,ref query,limit); }
        finally { ImGui.PopStyleVar(); }
        var center=pos+new Vector2(17,17)*scale;
        var dl=ImGui.GetWindowDrawList();
        dl.AddCircle(center,5*scale,0xffb9ada6,16,1.4f*scale);
        dl.AddLine(center+new Vector2(3)*scale,center+new Vector2(8)*scale,0xffb9ada6,1.5f*scale);
        if(query.Length>0){ImGui.SameLine();if(ImGui.Button("Clear###clear"+id)){query="";changed=true;}}
        return changed;
    }

    public static bool Switch(string id, bool value)
    {
        float h = 23*ImGui.GetTextLineHeight()/16f;
        var p = ImGui.GetCursorScreenPos();
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,h);
        ImGui.PushStyleColor(ImGuiCol.Button,value ? new Vector4(.13f,.56f,.54f,1) : new(.24f,.25f,.29f,1));
        try { if (ImGui.Button("###"+id,new Vector2(h*40/23,h))) value = !value; }
        finally { ImGui.PopStyleColor(); ImGui.PopStyleVar(); }
        ImGui.GetWindowDrawList().AddCircleFilled(p+new Vector2(value?h*(40f/23-.5f):h*.5f,h*.5f),h*.38f,0xfff4f5f5);
        return value;
    }

    public static string Choice(string label,string current,params string[] options)
    {
        if (!ImGui.BeginCombo(label,current)) return current;
        try
        {
            foreach (var option in options)
                if (ImGui.Selectable(option,current==option)) current=option;
        }
        finally { ImGui.EndCombo(); }
        return current;
    }

    public static void Integer(ref int value,int min,int max)
    {
        if (ImGui.InputInt("##number",ref value,0,0)) value=Math.Clamp(value,min,max);
    }

    public static void Amount(ref int value,int min,int max,string unit)
    {
        float available=ImGui.GetContentRegionAvail().X;
        ImGui.SetNextItemWidth(Math.Min(94*ImGui.GetTextLineHeight()/16f,Math.Max(40,available-ImGui.CalcTextSize(unit).X-10)));
        Integer(ref value,min,max);
        ImGui.SameLine(); ImGui.TextDisabled(unit);
    }

    public static bool Text(ref string value, int capacity = 160) =>
        ImGui.InputText("##text", ref value, capacity);

    public static void Decimal(ref float value, float min, float max)
    {
        float edited = value;
        if (ImGui.InputFloat("##decimal", ref edited) && float.IsFinite(edited))
            value = Math.Clamp(edited, min, max);
    }

    public static void Slider(ref int value, int min, int max, string unit)
    {
        string suffix = unit.Replace("%", "%%");
        if (ImGui.SliderInt("##slider", ref value, min, max, "%d " + suffix))
            value = Math.Clamp(value,min,max);
    }

    public static void Rgb(ref Vector3 value) => Rgb(ref value,null);

    public static void Rgb(ref Vector3 value,Action<Vector3>? drawSample)
    {
        float scale=ImGui.GetTextLineHeight()/16f;
        var color=Vector3.Clamp(value,Vector3.Zero,Vector3.One);
        var hex=$"#{(int)MathF.Round(color.X*255):X2}{(int)MathF.Round(color.Y*255):X2}{(int)MathF.Round(color.Z*255):X2}";
        var pos=ImGui.GetCursorScreenPos();
        float height=ImGui.GetFrameHeight();
        if(ImGui.Button("###rgb-field",new Vector2(116*scale,height))) ImGui.OpenPopup("RGB picker");
        var dl=ImGui.GetWindowDrawList();
        var swatch=pos+new Vector2(8*scale,(height-16*scale)/2);
        dl.AddRectFilled(swatch,swatch+new Vector2(16*scale),ImGui.GetColorU32(new Vector4(color,1)),3*scale);
        dl.AddRect(swatch,swatch+new Vector2(16*scale),0x60ffffff,3*scale);
        dl.AddText(pos+new Vector2(31*scale,(height-ImGui.GetTextLineHeight())/2),ImGui.GetColorU32(ImGuiCol.Text),hex);
        var arrow=pos+new Vector2(103*scale,height/2);
        dl.AddLine(arrow-new Vector2(4,2)*scale,arrow+new Vector2(0,2)*scale,0xffcfc6b6,1.4f*scale);
        dl.AddLine(arrow+new Vector2(0,2)*scale,arrow+new Vector2(4,-2)*scale,0xffcfc6b6,1.4f*scale);
        if(!ImGui.BeginPopup("RGB picker")) return;
        try
        {
            ImGui.SetNextItemWidth(192*scale);
            ImGui.ColorPicker3("##picker",ref value,ImGuiColorEditFlags.NoSidePreview|ImGuiColorEditFlags.DisplayHex);
            if(drawSample!=null){ ImGui.Separator();drawSample(value); }
        }
        finally { ImGui.EndPopup(); }
    }

    public static void ConfirmationBody(string question, string detail, string commit, Action accept, Action cancel)
    {
        ImGui.TextWrapped(question);
        ImGui.TextWrapped(detail);
        ImGui.Spacing();
        if (ImGui.Button(commit)) accept();
        ImGui.SameLine();
        if (ImGui.Button("Cancel") || ImGui.IsKeyPressed(ImGuiKey.Escape)) cancel();
    }

    public static void Multiple(string label,IEnumerable<string> options,ISet<string> selection)
    {
        if (!ImGui.BeginCombo(label,selection.Count==0?"None selected":string.Join(", ",selection))) return;
        try
        {
            foreach (var option in options)
            {
                bool on=selection.Contains(option);
                if (!ImGui.Checkbox(option,ref on)) continue;
                if (on) selection.Add(option); else selection.Remove(option);
            }
        }
        finally { ImGui.EndCombo(); }
    }

    public static void Confirm(string button,string question,string commit,Action accepted)
    {
        ImGui.PushID(button);
        try
        {
            if (ImGui.Button(button)) ImGui.OpenPopup("Confirm");
            if (!ImGui.BeginPopup("Confirm")) return;
            try
            {
                ImGui.TextWrapped(question);
                if (ImGui.Button(commit)) { accepted(); ImGui.CloseCurrentPopup(); }
                ImGui.SameLine();
                if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
            }
            finally { ImGui.EndPopup(); }
        }
        finally { ImGui.PopID(); }
    }
}
