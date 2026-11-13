namespace AetherglassUI;

/// <summary>Drafts stay bound to a named owner and revision until applied or discarded.</summary>
public sealed class ScopeDraft<T>(Func<T,T> copy,Func<T,T,bool> equal)
{
    private string? owner;
    private long revision;
    private bool originalOverride;
    private T original=default!;
    public bool Overridden {get;private set;}
    public T Value {get;set;}=default!;
    public bool Dirty=>owner!=null&&(Overridden!=originalOverride||(Overridden&&!equal(original,Value)));
    public bool Stale {get;private set;}
    public string Owner=>owner??"";
    public bool Load(string scope,long version,bool enabled,T value,bool discard=false)
    {
        if(!discard&&Dirty)
        {
            if(scope==owner&&version!=revision)Stale=true;
            return scope==owner&&!Stale;
        }
        if(!discard&&scope==owner&&revision==version&&enabled==originalOverride&&equal(original,value)&&!Stale)return true;
        owner=scope;revision=version;originalOverride=Overridden=enabled;
        original=copy(value);Value=copy(value);Stale=false;return true;
    }
    public bool LoadWithParent(string scope,long version,bool enabled,T savedValue,T parent,bool discard=false)
    {
        bool loaded=Load(scope,version,enabled,enabled?savedValue:parent,discard);
        if(loaded&&!Overridden&&!equal(Value,parent))Value=copy(parent);
        return loaded;
    }
    public void Override(bool enabled,T parent){if(enabled!=Overridden){Overridden=enabled;Value=copy(parent);}}
    public bool Apply(Func<string,long,bool,T,bool> save)
    {
        if(!Dirty||Stale)return false;
        if(!save(Owner,revision,Overridden,copy(Value))){Stale=true;return false;}
        owner=null;return true;
    }
}
