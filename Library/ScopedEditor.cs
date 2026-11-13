using Dalamud.Bindings.ImGui;

namespace FamilyUI;

/// <summary>When an override starts, copy the effective value. Mutable values require a deep copy.</summary>
public sealed class ScopedEditor<T>(Func<T,T> copy)
{
    private T local = default!;
    public bool Overridden { get; private set; }
    public T Effective(T parent) => Overridden ? local : parent;
    public void SetOverride(bool enabled, T parent)
    {
        if (enabled == Overridden) return;
        local = enabled ? copy(parent) : default!;
        Overridden = enabled;
    }

    public void Draw(string scope, string parentScope, T parent, Func<T,T> edit)
    {
        bool enabled = Overridden;
        if (ImGui.Checkbox("Override for "+scope,ref enabled)) SetOverride(enabled,parent);
        ImGui.TextDisabled("Source: "+(Overridden ? scope : parentScope));
        ImGui.BeginDisabled(!Overridden);
        try
        {
            var edited = edit(Effective(parent));
            if (Overridden) local = edited;
        }
        finally { ImGui.EndDisabled(); }
    }
}

public static class OrderedEditor
{
    public static void Move<T>(IList<T> source, int index, IList<T> destination, int position)
    {
        if (index < 0 || index >= source.Count) throw new ArgumentOutOfRangeException(nameof(index));
        int max = destination.Count - (ReferenceEquals(source,destination) ? 1 : 0);
        if (position < 0 || position > max) throw new ArgumentOutOfRangeException(nameof(position));
        T entry = source[index];
        source.RemoveAt(index);
        destination.Insert(position,entry);
    }

    public static int ReorderButtons(int index,int count)
    {
        int target = index;
        ImGui.BeginDisabled(index == 0);
        try { if(ImGui.SmallButton("Up")) target--; } finally { ImGui.EndDisabled(); }
        ImGui.SameLine();
        ImGui.BeginDisabled(index == count-1);
        try { if(ImGui.SmallButton("Down")) target++; } finally { ImGui.EndDisabled(); }
        return target;
    }
}
