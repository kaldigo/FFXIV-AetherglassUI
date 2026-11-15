using System.Numerics;
using AetherglassUI;
using Aetherloom;
using Dalamud.Bindings.ImGui;

internal sealed class WardrobeFixture:IWardrobe
{
    private readonly Dictionary<uint,Picture> art=[];
    private readonly List<WardrobeSlot> rows=[];
    private readonly List<(Guid Id,string Name,string[] Tags)> designs=[];
    private readonly Dictionary<string,byte> chosen=[];
    private readonly List<WardrobeDye> palette=[];
    public WardrobeLayout Layout=null!;
    public ImTextureID? HeaderMark {get;}
    public Guid Selected{get;private set;}
    public string Title=>designs.FirstOrDefault(d=>d.Id==Selected).Name??"";
    public string[] Tags=>designs.FirstOrDefault(d=>d.Id==Selected).Tags??[];
    public IReadOnlyList<string> KnownTags=>designs.SelectMany(d=>d.Tags).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    public IEnumerable<string> JobTerms(Guid id)=>["Bard","BRD"];
    public string Message=>"";
    public bool ImportBusy=>false;
    public string ImportStatus {get;private set;}="";
    public void StartImport(bool fromPlates,string url,string parent)=>ImportStatus="Imports are available in game.";
    private readonly List<string> folders=["[AL] Adventuring","[AL] Adventuring/Winter"];
    private readonly Dictionary<Guid,string> paths=[];
    public IReadOnlyCollection<string> Folders=>folders;
    public IEnumerable<LibraryDesign> Designs=>designs.Select(d=>new LibraryDesign(d.Id,d.Name,d.Tags,paths.GetValueOrDefault(d.Id)??""));
    public IEnumerable<WardrobeDye> Dyes=>palette;
    private readonly byte[] appearancePalette=new byte[4096];
    public WardrobeFixture(Func<string,ImTextureID> load)
    {
        HeaderMark=load("mark");
        var gear=load("gear");var look=load("appearance");var acc=load("accessories");
        Picture Cut(ImTextureID t,float x,float y,float w,float h)=>new(t,new(x/1585,y/992),new((x+w)/1585,(y+h)/992));
        art[100]=Cut(gear,960,58,620,843);art[101]=Cut(look,960,58,620,849); art[102]=Cut(acc,1020,60,560,840);
        string[] keys=["MainHand","Head","Body","Hands","Legs","Feet","Ears","Neck","Wrists","RFinger","LFinger"];
        string[] labels=["Main hand","Head","Body","Hands","Legs","Feet","Earrings","Necklace","Bracelets","Right ring","Left ring"];
        string[] names=["Longbow","Woolen Beret","Expedition Coat","Leather Gloves","Wool Tights","Expedition Boots","Silver Ear Cuffs","Wayfarer’s Choker","Leather Wristband","Silver Band","Amber Ring"];
        for(int i=0;i<keys.Length;i++){
            uint id=(uint)(i+1);
            art[id]=i<6?Cut(gear,470,new[]{277,363,448,535,621,700}[i],81,61):Cut(acc,542,275+(i-6)*111,103,77);
            rows.Add(new(keys[i],labels[i],new(id,names[i],id,i>=9?0:2,i==0?"Bard":""),52,2,true));
        }
        var styles=load("appearance-pickers");
        var items=load("gear-picker");
        for(int i=0;i<12;i++){
            float y=273+94*(i/4);
            Picture StyleTile(float x)=>new(styles,new(x/2172,y/724),new((x+66)/2172,(y+66)/724));
            art[(uint)(60+i)]=StyleTile(445+79*(i%4));
            art[(uint)(80+i)]=StyleTile(1531+79*(i%4));
        }
        for(int i=0;i<3;i++)art[(uint)(120+i)]=Cut(items,587,528+i*83,60,60);
        foreach(var name in new[]{"Highland Scout","Rainy Afternoon","Winter Courier","Moonlit Waltz"})designs.Add((Guid.NewGuid(),name,["aetherloom","Winter","Travel"]));
        designs[1]=(designs[1].Id,designs[1].Name,["aetherloom","Casual","City"]);
        Selected=designs[0].Id;assigned[("Jobs",1)]=Selected;assigned[("Zones",1)]=Selected;assigned[("Jobs",2)]=designs[1].Id;paths[Selected]="[AL] Adventuring";paths[designs[2].Id]="[AL] Adventuring/Winter";
        using var samples=typeof(WardrobeFixture).Assembly.GetManifestResourceStream("appearance-palette.bin")!;
        samples.ReadExactly(appearancePalette);
        using var stream=typeof(WardrobeFixture).Assembly.GetManifestResourceStream("stains.tsv")!;
        using var reader=new StreamReader(stream);
        while(reader.ReadLine() is {} line)
        {
            var parts=line.Split('\t');
            palette.Add(new(byte.Parse(parts[0]),parts[1],uint.Parse(parts[2]),WardrobeDye.FamilyFromShade(int.Parse(parts[3]))));
        }

    }
    public string Availability(uint id)=>id is 4 or 11 or 25?"Missing":"Glamour Dresser";
    public Picture? Icon(uint id)=>art.TryGetValue(id,out var img)?img:null;
    public void Select(Guid id)=>Selected=id;
    public bool Create(string name,string folder){Selected=Guid.NewGuid();designs.Add((Selected,name,["aetherloom"]));paths[Selected]=folder;return true;}
    public bool CreateFolder(string parent,string name){folders.Add(parent.Length==0?"[AL] "+name:parent+"/"+name);return true;}
    public bool RelocateFolder(string source,string parent,string name)
    {
        var destination=WardrobeLibrary.PathFor(parent,name);
        if(WardrobeLibrary.Within(parent,source) || destination!=source && folders.Contains(destination))return false;
        for(int i=0;i<folders.Count;i++)
            if(WardrobeLibrary.Within(folders[i],source))folders[i]=destination+folders[i][source.Length..];
        foreach(var (id,path) in paths.ToArray())
            if(WardrobeLibrary.Within(path,source))paths[id]=destination+path[source.Length..];
        return true;
    }
    public bool DeleteFolder(string path)
    {
        var parent=WardrobeLibrary.Parent(path);
        var children=folders.Where(f=>WardrobeLibrary.Parent(f)==path).ToArray();
        if(children.Any(c=>folders.Contains(WardrobeLibrary.PathFor(parent,WardrobeLibrary.Label(c)))))return false;
        foreach(var child in children)RelocateFolder(child,parent,WardrobeLibrary.Label(child));
        foreach(var (id,old) in paths.ToArray())if(old==path)paths[id]=parent;
        return folders.Remove(path);
    }
    public void MoveDesign(Guid id,string folder)=>paths[id]=folder;
    public void CloneDesign(Guid source,string name)
    {
        var entry=Designs.Single(d=>d.Id==source);
        Selected=Guid.NewGuid();
        designs.Add((Selected,name,[..entry.Tags]));
        paths[Selected]=entry.Folder;
        if(bypassed.Contains(source))bypassed.Add(Selected);
    }
    public bool RenameDesign(Guid id,string name)
    {
        var index=designs.FindIndex(d=>d.Id==id);if(index<0)return false;
        designs[index]=(id,name,designs[index].Tags);return true;
    }
    public bool DeleteDesign(Guid id)
    {
        if(designs.RemoveAll(d=>d.Id==id)==0)return false;
        paths.Remove(id);
        foreach(var entry in assigned.Where(a=>a.Value==id).ToArray())assigned.Remove(entry.Key);
        if(Selected==id)Selected=designs.FirstOrDefault().Id;
        return true;
    }
    public void Tag(string tag,bool add){var i=designs.FindIndex(x=>x.Id==Selected);designs[i]=(Selected,Title,add?[..Tags,tag]:Tags.Where(x=>x!=tag).ToArray());}
    public IEnumerable<WardrobeSlot> Slots(bool accessories)=>rows.Where(x=>accessories==(x.Key is "Ears" or "Neck" or "Wrists" or "RFinger" or "LFinger"));
    public IEnumerable<WardrobeItem> Catalog(string key){
        var item=rows.First(r=>r.Key==key).Item;yield return item;
        if(key!="Body")yield break;
        foreach(var (name,id) in new[]{("Highland Smock",22u),("Sky Pirate’s Jacket",23u),("Adventuring Sweater",25u)})yield return new(id,name,key=="Body"?(id==25?122u:id+98):item.Icon,2);
    }
    private void Change(string key,Func<WardrobeSlot,WardrobeSlot> change){int i=rows.FindIndex(x=>x.Key==key);rows[i]=change(rows[i]);}
    public void SetItem(string key,uint id)=>Change(key,s=>s with{Item=id==0?new(0,"Empty",0,0):Catalog(key).First(i=>i.Id==id),First=id==0?(byte)0:s.First,Second=id==0?(byte)0:s.Second,Apply=true});
    public void SetDye(string key,int channel,byte id)=>Change(key,s=>channel==0?s with{First=id}:s with{Second=id});
    public void SetApply(string key,bool apply)=>Change(key,s=>s with{Apply=apply});
    public void MainHand(string? remove){if(remove!=null)rows.RemoveAll(x=>x.Key==remove);else rows.Insert(1,new("MainHand/"+Guid.NewGuid(),"Main hand",new(0,"Empty",0,0),0,0,false));}
    public IEnumerable<WardrobeStyle> Styles(string key)=>Enumerable.Range(0,12).Select(i=>new WardrobeStyle((byte)i,$"{(key=="Hairstyle"?"Hairstyle":"Pattern")} {i+1}",(uint)((key=="Hairstyle"?60:80)+i)));
    public uint AppearanceColor(string key,byte value)
    {
        var index=key switch{"HairColor"=>value,"HighlightsColor"=>256+value,_=>value<128?512+value:768+value-128};
        return BitConverter.ToUInt32(appearancePalette,index*4)|0xff000000;
    }
    public byte Style(string key)=>chosen.GetValueOrDefault(key);
    public void SetStyle(string key,byte id)=>chosen[key]=id;
    private readonly HashSet<string> enabled=["Hair","Face paint"];
    public bool GroupEnabled(string group)=>enabled.Contains(group);
    public void SetGroupEnabled(string group,bool value){if(value)enabled.Add(group);else enabled.Remove(group);}
    public bool HasCharacter=>true;
    private readonly HashSet<Guid> bypassed=[];
    public bool BypassOwnership {get=>bypassed.Contains(Selected);set{if(value)bypassed.Add(Selected);else bypassed.Remove(Selected);}}
    public void ApplyToCharacter(){}
    private readonly Dictionary<(string Type,uint Id),Guid> assigned=[];
    public IEnumerable<(uint Id,string Name)> AssignmentOptions(string type)=>type=="Jobs"?[(1,"Bard (BRD)"),(2,"Dragoon (DRG)"),(3,"Warrior (WAR)")]:[(1,"Coerthas Central Highlands"),(2,"Old Gridania"),(3,"The Lavender Beds")];
    public IReadOnlyCollection<uint> Assigned(string type)=>assigned.Where(p=>p.Key.Type==type && p.Value==Selected).Select(p=>p.Key.Id).ToArray();
    public string? AssignmentOwner(string type,uint id)=>designs.FirstOrDefault(d=>d.Id==assigned.GetValueOrDefault((type,id))).Name;
    public void Assign(string type,uint id,bool enable){if(enable)assigned[(type,id)]=Selected;else assigned.Remove((type,id));}
    public ISet<byte> FavoriteDyes{get;}=new HashSet<byte>{5,11,14,21,32};
    public void ToggleFavorite(byte id){if(!FavoriteDyes.Remove(id))FavoriteDyes.Add(id);}
    private readonly Dictionary<string,bool?> display=new(){["Weapon"]=true,["Hat"]=true,["Visor"]=false};
    public bool? DisplayOption(string key)=>display.GetValueOrDefault(key);
    public void ToggleDisplay(string key)=>display[key]=key=="Draw"?DisplayOption(key)!=true:DisplayOption(key) switch{null=>true,true=>false,false=>null};
    public void ResetCamera(){}
    public void WeaponSelector(){}
    public void DrawPreview(){
        uint id=Layout.Tab switch{"Appearance"=>101u,"Accessories"=>102u,_=>100u};
        float aspect=id==102?2f/3:.735f;
        WorkspaceControls.FillViewport("preview",art[id],aspect,(_,_)=>{});
    }
}
