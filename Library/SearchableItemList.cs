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
        float scale = ImGui.GetTextLineHeight() / 16f;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(14, 12) * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 6 * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 6) * scale);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, 0x85170f0c);
        bool visible = ImGui.BeginChild("item-picker", new Vector2(0, 252 * scale), false,
            ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        try
        {
            if (!visible) return;
            Controls.Search("##item-search", "Find an item by name or ID...", ref query);
            DrawResults(configured, searchCatalog, open, configuredLabel, catalogLabel, scale);
        }
        finally
        {
            ImGui.EndChild();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(3);
        }
    }

    private void DrawResults(IEnumerable<ItemListEntry> configured, Func<string, IReadOnlyList<ItemListEntry>> searchCatalog,
        Action<uint> open, string configuredLabel, string catalogLabel, float scale)
    {
        string term = query.Trim();
        bool changed = term != lastQuery;
        if (changed)
        {
            suggestions = term.Length >= 2 ? searchCatalog(term) : [];
            lastQuery = term;
        }
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        bool shown = ImGui.BeginChild("item-results", Vector2.Zero, false);
        ImGui.PopStyleColor();
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
            if (saved.Length > 0) DrawEntries(configuredLabel, saved, open, scale);
            if (available.Length > 0) DrawEntries(catalogLabel, available, open, scale);
            if (saved.Length == 0 && available.Length == 0)
                ImGui.TextWrapped(term.Length < 2 ? "Search with at least two characters to add an item." : "No matching items.");
        }
        finally { ImGui.EndChild(); }
    }

    private static void DrawEntries(string label, ItemListEntry[] entries, Action<uint> open, float scale)
    {
        ImGui.TextDisabled(label);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, 0x453f8579);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, 0x80519488);
        try
        {
            foreach (var entry in entries)
            {
                var start = ImGui.GetCursorScreenPos();
                float width = ImGui.GetContentRegionAvail().X;
                bool choose = ImGui.Selectable($"##item-{entry.Id}", false, ImGuiSelectableFlags.None,
                    new Vector2(width, 32 * scale));
                var dl = ImGui.GetWindowDrawList();
                float center = start.Y + 16 * scale;
                float right = start.X + width - 10 * scale;
                string id = entry.Id.ToString();
                float idX = right - 17 * scale - ImGui.CalcTextSize(id).X;
                var namePos = new Vector2(start.X + 8 * scale, center - ImGui.GetTextLineHeight() / 2);
                dl.PushClipRect(start, new Vector2(Math.Max(start.X, idX - 10 * scale), start.Y + 32 * scale), true);
                dl.AddText(namePos, ImGui.GetColorU32(ImGuiCol.Text), entry.Name);
                dl.PopClipRect();
                dl.AddText(new Vector2(idX, namePos.Y), ImGui.GetColorU32(ImGuiCol.TextDisabled), id);
                uint accent = ImGui.IsItemHovered() || ImGui.IsItemFocused() ? 0xffced462 : 0xffaa9e92;
                dl.AddLine(new(right-4*scale,center-4*scale),new(right,center),accent,1.4f*scale);
                dl.AddLine(new(right,center),new(right-4*scale,center+4*scale),accent,1.4f*scale);
                dl.AddLine(new(namePos.X,start.Y+32*scale),new(right,start.Y+32*scale),0x19e8d3c9);
                if (Controls.HintRequested() && ImGui.CalcTextSize(entry.Name).X > idX - 10 * scale - namePos.X)
                    ImGui.SetTooltip(entry.Name);
                if (choose) open(entry.Id);
            }
        }
        finally { ImGui.PopStyleColor(2); ImGui.PopStyleVar(); }
        ImGui.Dummy(new Vector2(0, 2 * scale));
    }
}
