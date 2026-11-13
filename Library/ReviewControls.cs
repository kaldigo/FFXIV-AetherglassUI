using Dalamud.Bindings.ImGui;

namespace AetherglassUI;

public enum StateTone { Ready, Pending, Attention, Error }
public sealed record ReviewStatus(string Text,StateTone Tone=StateTone.Ready,Action? Review=null)
{
    public void Draw()
    {
        if(Review!=null){if(ImGui.SmallButton("Review"))Review();ImGui.SameLine();}
        ImGui.TextUnformatted(Text);
    }
}

public static class ReviewControls
{
    public static void Metric(string id,string label,int? value,string detail,Action? inspect=null)
    {
        Controls.Row(id,label,detail,()=>{
            if(inspect==null)ImGui.TextUnformatted(value?.ToString("N0")??"Unknown");
            else if(ImGui.Button((value?.ToString("N0")??"Unknown")+" · Review"))inspect();
        });
    }
    public static string Filters(string selected,IEnumerable<(string Key,int Count)> filters)
    {
        bool first=true;
        float right=ImGui.GetCursorScreenPos().X+ImGui.GetContentRegionAvail().X;
        foreach(var (key,count) in filters)
        {
            string label=$"{key} ({count})";
            if(!first&&ImGui.GetItemRectMax().X+ImGui.CalcTextSize(label).X+30<=right)ImGui.SameLine();
            if(key==selected)ImGui.PushStyleColor(ImGuiCol.Button,0x806e7442);
            bool clicked=ImGui.SmallButton(label+"###"+key);
            if(key==selected)ImGui.PopStyleColor();
            if(clicked)selected=key;
            first=false;
        }
        return selected;
    }
}

/// <summary>The caller keeps the query, filters and selected record alongside this navigation state.</summary>
public sealed class DetailNavigation<T> where T:class
{
    public T? Record {get;private set;}
    private float scroll;
    private bool restore;
    public void Open(T record)=>Open(record,ImGui.GetScrollY());
    public void Open(T record,float listScroll){scroll=listScroll;Record=record;}
    public void Reset(){Record=null;scroll=0;restore=true;}
    public void Back(){Record=null;restore=true;}
    public void RestoreList(){if(restore){ImGui.SetScrollY(scroll);restore=false;}}
}
