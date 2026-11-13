using Dalamud.Bindings.ImGui;

namespace FamilyUI;

public sealed record CatalogEntry(string Id,string Name,string Group,string Keywords="",Action? DrawIcon=null);

/// <summary>Pass a stable catalog instance, replacing it when its entries change.</summary>
public sealed class CatalogPicker
{
    public string Query="";
    private IReadOnlyList<CatalogEntry>? source;
    private Dictionary<string,CatalogEntry> index=[];
    private string? lastQuery;
    private CatalogEntry[] found=[];
    private int page;
    private const int PageSize=32;

    private void Index(IReadOnlyList<CatalogEntry> catalog)
    {
        if(ReferenceEquals(source,catalog))return;
        index=catalog.ToDictionary(e=>e.Id);source=catalog;lastQuery=null;
    }

    public IReadOnlyList<CatalogEntry> Matches(IReadOnlyList<CatalogEntry> catalog,ISet<string> selected)
    {
        Index(catalog);
        bool Match(CatalogEntry e,string[] words)=>words.All(w=>e.Name.Contains(w,StringComparison.OrdinalIgnoreCase)||
            e.Id.Contains(w,StringComparison.OrdinalIgnoreCase)||e.Group.Contains(w,StringComparison.OrdinalIgnoreCase)||
            e.Keywords.Contains(w,StringComparison.OrdinalIgnoreCase));
        var words=Query.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
        if(lastQuery!=Query)
        {
            found=words.Length==0?catalog.ToArray():catalog.Where(e=>Match(e,words)).ToArray();
            lastQuery=Query;page=0;
        }
        var missing=selected.Where(id=>!index.ContainsKey(id)).Select(id=>new CatalogEntry(id,"Unavailable","Unavailable selections")).Where(e=>Match(e,words)).ToArray();
        return missing.Length==0?found:[..missing,..found];
    }

    public void Draw(string id,IReadOnlyList<CatalogEntry> catalog,ISet<string> selected,string empty,bool single=false)
    {
        string summary=empty;
        if(selected.Count==1){Index(catalog);var key=selected.First();summary=index.GetValueOrDefault(key)?.Name??$"Unavailable ({key})";}
        else if(selected.Count>1)summary=$"{selected.Count} selected";
        if(!ImGui.BeginCombo(id,summary))return;
        try
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##lookup","Find by name or ID...",ref Query,120);
            var matches=Matches(catalog,selected);
            if(matches.Count==0){ImGui.TextDisabled("No matching entries.");return;}
            int last=(matches.Count-1)/PageSize;
            page=Math.Clamp(page,0,last);
            if(last>0)
            {
                ImGui.TextDisabled($"{matches.Count:N0} matches · Page {page+1} of {last+1}");
                ImGui.BeginDisabled(page==0);if(ImGui.SmallButton("Previous"))page--;ImGui.EndDisabled();
                ImGui.SameLine();ImGui.BeginDisabled(page==last);if(ImGui.SmallButton("Next"))page++;ImGui.EndDisabled();
            }
            foreach(var group in matches.Skip(page*PageSize).Take(PageSize).GroupBy(e=>e.Group))
            {
                ImGui.PushID(group.Key);
                ImGui.TextUnformatted(group.Key);
                if(!single)
                {
                    if(ImGui.SmallButton("Select shown"))selected.UnionWith(group.Where(e=>index.ContainsKey(e.Id)).Select(e=>e.Id));
                    ImGui.SameLine();if(ImGui.SmallButton("Clear shown"))selected.ExceptWith(group.Select(e=>e.Id));
                }
                foreach(var entry in group)
                {
                    entry.DrawIcon?.Invoke();if(entry.DrawIcon!=null)ImGui.SameLine();
                    bool was=selected.Contains(entry.Id);
                    if(!ImGui.Selectable($"{entry.Name}  ({entry.Id})###{entry.Id}",was,ImGuiSelectableFlags.DontClosePopups))continue;
                    if(single)selected.Clear();
                    if(was)selected.Remove(entry.Id);else if(index.ContainsKey(entry.Id))selected.Add(entry.Id);
                    if(single)ImGui.CloseCurrentPopup();
                }
                ImGui.PopID();
            }
        }
        finally{ImGui.EndCombo();}
    }
}
