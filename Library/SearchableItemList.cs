using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AetherglassUI;

public sealed record ItemListEntry(uint Id, string Name);

/// <summary>Keep one instance on the inventory page so the filter and list offset survive editing.</summary>
public sealed class SearchableItemList
{
    private string query = "";
    private string? lastQuery;
    private IReadOnlyList<ItemListEntry> suggestions = [];
    private float? restoreScroll;
    public string Query { get => query; set => query = value ?? ""; }
    public float ScrollPosition { get; private set; }
    public void RestoreScroll(float position) => restoreScroll = position;

    /// <summary>Open a record editor and restore both scroll regions when its Back button is used.</summary>
    public void OpenDetail(WindowShell window, SettingsPage parent, string id, string label, Action draw)
    {
        float pageOffset = parent.ScrollPosition, listOffset = ScrollPosition;
        window.OpenSubpage(new Destination(id, label, "", draw)
        {
            OnBack = () => { parent.RestoreScroll(pageOffset); RestoreScroll(listOffset); }
        });
    }

    public void Draw(IEnumerable<ItemListEntry> configured, Func<string, IReadOnlyList<ItemListEntry>> searchCatalog,
        Action<uint> open, string configuredLabel = "Configured items", string catalogLabel = "Other items")
    {
        Controls.Search("##item-search", "Find an item by name or ID...", ref query);
        string term = query.Trim();
        bool changed = term != lastQuery;
        if (changed)
        {
            suggestions = term.Length >= 2 ? searchCatalog(term) : [];
            lastQuery = term;
        }
        float scale = ImGui.GetTextLineHeight() / 16f;
        bool shown = ImGui.BeginChild("item-results", new Vector2(0, 176 * scale), true, ImGuiWindowFlags.AlwaysUseWindowPadding);
        try
        {
            if (restoreScroll.HasValue || changed)
                ImGui.SetScrollY(restoreScroll ?? 0);
            restoreScroll = null;
            ScrollPosition = ImGui.GetScrollY();
            if (!shown) return;
            var saved = configured.Where(e => e.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || e.Id.ToString().Contains(term)).OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(e => e.Id).ToArray();
            var savedIds = saved.Select(e => e.Id).ToHashSet();
            var available = suggestions.Where(e => !savedIds.Contains(e.Id)).ToArray();
            if (saved.Length > 0) DrawEntries(configuredLabel, saved, open);
            if (available.Length > 0) DrawEntries(catalogLabel, available, open);
            if (saved.Length == 0 && available.Length == 0)
                ImGui.TextWrapped(term.Length < 2 ? "Search with at least two characters to add an item." : "No matching items.");
        }
        finally { ImGui.EndChild(); }
    }

    private static void DrawEntries(string label, IEnumerable<ItemListEntry> entries, Action<uint> open)
    {
        ImGui.TextDisabled(label);
        foreach (var entry in entries)
            if (ImGui.Selectable($"{entry.Name}  ({entry.Id})###{entry.Id}")) open(entry.Id);
    }
}
