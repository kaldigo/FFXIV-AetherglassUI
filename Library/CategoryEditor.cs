using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AetherglassUI;

public sealed record CategoryOption(string Name,string Group,IReadOnlyList<string> Members,string Keywords="",bool CanAdd=true,ImTextureID? Icon=null);

/// <summary>Keep one editor per field. A display choice may represent multiple saved IDs.</summary>
public sealed class CategoryEditor
{
    public bool Expanded { get; set; }
    public string Query="";
    public static void Set(ISet<string> values,IEnumerable<CategoryOption> options,bool selected)
    {
        foreach(var option in options)
            if(!selected)values.ExceptWith(option.Members);
            else if(option.CanAdd)values.UnionWith(option.Members);
    }

    public Setting Row(string id,string label,IReadOnlyList<CategoryOption> options,ISet<string> values,string empty,string detail,Action? changed=null)
    {
        CategoryOption[] choices=[];
        return new Setting(id,label,()=>{
            var known=options.SelectMany(x=>x.Members).ToHashSet();
            var missing=values.Where(v=>!known.Contains(v)).Select(v=>new CategoryOption("Unavailable "+v,"Unavailable",[v],CanAdd:false));
            choices=options.Where(x=>x.CanAdd||x.Members.Any(values.Contains)).Concat(missing).ToArray();
            var picked=choices.Where(x=>x.Members.Any(values.Contains)).ToArray();
            string summary=picked.Length switch {0=>empty,1=>picked[0].Name,_=>$"{picked.Length} categories"};
            if(picked.Any(x=>!x.Members.All(values.Contains)))summary+=" (partial)";
            float width=ImGui.CalcItemWidth();
            var pos=ImGui.GetCursorScreenPos();
            if(ImGui.Button("###expand",new(width,0)))Expanded=!Expanded;
            var end=ImGui.GetItemRectMax();
            var dl=ImGui.GetWindowDrawList();
            float unit=ImGui.GetTextLineHeight()/16f;
            dl.PushClipRect(pos,end-new Vector2(25*unit,0),true);
            dl.AddText(pos+new Vector2(10*unit,(end.Y-pos.Y-ImGui.GetTextLineHeight())/2),ImGui.GetColorU32(ImGuiCol.Text),summary);
            dl.PopClipRect();
            var mid=new Vector2(end.X-14*unit,(pos.Y+end.Y)/2);
            float sign=Expanded?-1:1;
            var tip=mid+new Vector2(0,2*sign)*unit;
            dl.AddLine(mid+new Vector2(-4,-2*sign)*unit,tip,0xffcfc6b6,1.4f*unit);
            dl.AddLine(tip,mid+new Vector2(4,-2*sign)*unit,0xffcfc6b6,1.4f*unit);
            if(ImGui.IsItemHovered()||ImGui.IsItemFocused())ImGui.SetTooltip(summary);
        },Detail:detail){ExpandedContent=()=>{if(Expanded)Draw(choices,values,changed);}};
    }

    private static void BulkMenu(string id,string label,string scopeLabel,CategoryOption[] scope,ISet<string> values,Action? changed)
    {
        if(ImGui.Button(label+"###"+id))ImGui.OpenPopup(id);
        if(!ImGui.BeginPopup(id))return;
        try
        {
            if(ImGui.MenuItem("Select "+scopeLabel)){Set(values,scope,true);changed?.Invoke();}
            if(ImGui.MenuItem("Clear "+scopeLabel)){Set(values,scope,false);changed?.Invoke();}
        }
        finally{ImGui.EndPopup();}
    }

    private void Draw(CategoryOption[] choices,ISet<string> values,Action? changed)
    {
        float unit=ImGui.GetTextLineHeight()/16f;
        var size=new Vector2(0,Math.Clamp(ImGui.GetContentRegionAvail().Y-52*unit,215*unit,420*unit));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding,new Vector2(14,10)*unit);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing,new Vector2(8,6)*unit);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding,6*unit);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize,0);
        ImGui.PushStyleColor(ImGuiCol.ChildBg,0x85170f0c);
        bool shown=ImGui.BeginChild("choices",size,true,ImGuiWindowFlags.AlwaysUseWindowPadding);
        try
        {
            if(!shown)return;
            bool searched=Controls.Search("##lookup","Find category by name or ID...",ref Query,120);
            var words=Query.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
            var matches=choices.Where(x=>words.All(w=>(x.Name+" "+x.Group+" "+x.Keywords+" "+string.Join(" ",x.Members)).Contains(w,StringComparison.OrdinalIgnoreCase))).ToArray();
            float right=ImGui.GetCursorScreenPos().X+ImGui.GetContentRegionAvail().X;
            ImGui.AlignTextToFramePadding();
            ImGui.TextDisabled(words.Length==0?$"{choices.Length} categories":$"{matches.Length} of {choices.Length} categories");
            AlignAction(right,"All categories");
            BulkMenu("all","All categories","all categories",choices,values,changed);
            ImGui.PushStyleColor(ImGuiCol.ChildBg,Vector4.Zero);
            bool grid=ScrollRegion.Begin("options");
            try
            {
                if(!grid)return;
                if(searched)ImGui.SetScrollY(0);
                if(matches.Length==0)ImGui.TextDisabled("No matching categories.");
                foreach(var group in matches.GroupBy(x=>x.Group))
                {
                    var entries=group.ToArray();
                    float width=ImGui.GetContentRegionAvail().X;
                    float groupRight=ImGui.GetCursorScreenPos().X+width;
                    ImGui.PushID(group.Key);
                    try
                    {
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted(group.Key+"  ·  "+entries.Length);
                        AlignAction(groupRight,"Group");
                        BulkMenu("shown","Group","shown in group",entries,values,changed);
                        int columns=Math.Max(1,(int)((width+8*unit)/(130*unit)));
                        float cell=(width-(columns-1)*8*unit)/columns;
                        for(int i=0;i<entries.Length;i++)
                        {
                            if(i%columns!=0)ImGui.SameLine();
                            Tile(entries[i],values,cell,unit,changed);
                        }
                        ImGui.Dummy(new Vector2(0,3*unit));
                    }
                    finally{ImGui.PopID();}
                }
            }
            finally{ScrollRegion.End();ImGui.PopStyleColor();}
        }
        finally{ImGui.EndChild();ImGui.PopStyleColor();ImGui.PopStyleVar(4);}
    }

    private static void AlignAction(float right,string label)
    {
        float x=right-ImGui.CalcTextSize(label).X-2*ImGui.GetStyle().FramePadding.X;
        if(ImGui.GetItemRectMax().X+ImGui.GetStyle().ItemSpacing.X<x){ImGui.SameLine();ImGui.SetCursorScreenPos(new(x,ImGui.GetCursorScreenPos().Y));}
    }

    private static void Tile(CategoryOption entry,ISet<string> values,float width,float unit,Action? changed)
    {
        int selected=entry.Members.Count(values.Contains);
        bool full=selected==entry.Members.Count;
        var pos=ImGui.GetCursorScreenPos();
        var size=new Vector2(width,48*unit);
        if(selected>0)ImGui.PushStyleColor(ImGuiCol.Button,full?0x405b8070u:0x384871adu);
        bool clicked=ImGui.Button("###"+entry.Members[0],size);
        if(selected>0)ImGui.PopStyleColor();
        if(clicked){Set(values,[entry],entry.CanAdd&&!full);changed?.Invoke();}
        var dl=ImGui.GetWindowDrawList();
        float left=9*unit;
        if(entry.Icon is {} icon){
            var at=pos+new Vector2(left,11*unit);
            dl.AddImage(icon,at,at+new Vector2(26*unit));
            left+=33*unit;
        }
        float wrap=Math.Max(1,width-left-7*unit);
        float height=Math.Min(2*ImGui.GetTextLineHeight(),ImGui.CalcTextSize(entry.Name,false,wrap).Y);
        var textAt=pos+new Vector2(left,(size.Y-height)/2);
        dl.PushClipRect(textAt,pos+new Vector2(width-5*unit,(size.Y+height)/2),true);
        dl.AddText(ImGui.GetFont(),ImGui.GetFontSize(),textAt,ImGui.GetColorU32(ImGuiCol.Text),entry.Name,wrap);
        dl.PopClipRect();
        if(selected>0)dl.AddRect(pos,pos+size,full?0xffced462:0xff60bef4,5*unit,ImDrawFlags.None,1.5f*unit);
        if(ImGui.IsItemHovered()||ImGui.IsItemFocused())ImGui.SetTooltip(entry.Name+" · "+(full?"Selected":selected>0?"Partially selected":"Unselected")+"\n"+string.Join(", ",entry.Members));
    }
}
