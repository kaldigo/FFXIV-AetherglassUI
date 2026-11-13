using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AetherglassUI;

public static class Editors
{
    public static void Table(string id,IReadOnlyList<string> headings,int rows,Action<int,int> cell)
        => Table(id,headings,rows,cell,null);

    public static void Table(string id,IReadOnlyList<string> headings,int rows,Action<int,int> cell,Func<int,string>? rowKey)
    {
        if (!ImGui.BeginTable(id,headings.Count,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerH|ImGuiTableFlags.SizingStretchSame)) return;
        try
        {
            foreach (string heading in headings) ImGui.TableSetupColumn(heading);
            ImGui.TableHeadersRow();
            for (int row=0;row<rows;row++)
            {
                if (rowKey is null) ImGui.PushID(row); else ImGui.PushID(rowKey(row));
                try
                {
                    for(int col=0;col<headings.Count;col++)
                    {
                        ImGui.TableNextColumn();ImGui.PushID(col);
                        try { ImGui.SetNextItemWidth(-1); cell(row,col); } finally { ImGui.PopID(); }
                    }
                }
                finally { ImGui.PopID(); }
            }
        }
        finally { ImGui.EndTable(); }
    }

    public static int Slots(string id,IReadOnlyList<string> labels,int selected)
    {
        ImGui.PushID(id);
        try
        {
            float width=ImGui.GetContentRegionAvail().X;
            int columns=Math.Clamp((int)(width/(ImGui.GetFrameHeight()*3.5f)),1,12);
            float cell=Math.Max(1,(width-(columns-1)*ImGui.GetStyle().ItemSpacing.X)/columns);
            for(int i=0;i<labels.Count;i++)
            {
                if(i%columns!=0) ImGui.SameLine();
                string label=labels[i].Length==0?"Empty":labels[i];
                if(ImGui.Button($"{(i==selected?"* ":"")}{i+1}\n{label}###slot{i}",new Vector2(cell,ImGui.GetFrameHeight()*2))) selected=i;
            }
        }
        finally { ImGui.PopID(); }
        return selected;
    }

    public static void DraftActions(string error,bool dirty,Action apply,Action discard)
    {
        if(error.Length>0)ImGui.TextWrapped(error);
        ImGui.BeginDisabled(!dirty||error.Length>0);
        try { if(ImGui.Button("Apply"))apply(); } finally { ImGui.EndDisabled(); }
        ImGui.SameLine();ImGui.BeginDisabled(!dirty);
        try { if(ImGui.Button("Discard"))discard(); } finally { ImGui.EndDisabled(); }
    }
}
