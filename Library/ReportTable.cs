using Dalamud.Bindings.ImGui;

namespace AetherglassUI;

public sealed record ReportColumn(string Label,bool Stretch=false);

public static class ReportTable
{
    public static void Draw(string id,IReadOnlyList<ReportColumn> columns,int count,Action<int,int> cell)
    {
        var flags=ImGuiTableFlags.RowBg|ImGuiTableFlags.Resizable|ImGuiTableFlags.ScrollY|ImGuiTableFlags.ScrollX;
        if(!ImGui.BeginTable(id,columns.Count,flags))return;
        try{
            ImGui.TableSetupScrollFreeze(0,1);
            foreach(var c in columns)ImGui.TableSetupColumn(c.Label,c.Stretch?ImGuiTableColumnFlags.WidthStretch:ImGuiTableColumnFlags.WidthFixed,c.Stretch?1:0);
            ImGui.TableHeadersRow();
            for(int i=0;i<count;i++){
                ImGui.TableNextRow();
                for(int j=0;j<columns.Count;j++){ImGui.TableSetColumnIndex(j);cell(i,j);}
            }
        }finally{ImGui.EndTable();}
    }
}
