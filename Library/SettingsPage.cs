using System.Numerics;
using Dalamud.Bindings.ImGui;
namespace AetherglassUI;

public enum FieldLayout { Flexible, Number, Switch, Color }
public sealed record Setting(string Id,string Label,Action Draw,string Keywords="",string Detail="",Func<bool>? Visible=null)
{
    public Func<string?>? DisabledReason { get; init; }
    public Action? ExpandedContent { get; init; }
    public FieldLayout Layout { get; init; }
}
public sealed record Topic(string Name,params Setting[] Rows);
public sealed record Group(string Id,string Name,params Topic[] Topics)
{
    public Action? Body { get; init; }
    public string Summary { get; init; }="";
    public string Status { get; init; }="";
    public string? DefaultTopic { get; init; }
}
public sealed class SettingsPage
{
    private string query="";
    private bool focusSearch;
    private string lastQuery="";
    private float browseScroll;
    private readonly HashSet<string> collapsed=[];
    private readonly Dictionary<string,string> selected=[];
    public string Query {get=>query; set=>query=value??"";}
    public float ScrollPosition {get;private set;}
    public int LastRowCount {get;private set;}
    private string? scrollTarget;
    public void ScrollToGroup(string id)=>scrollTarget=id;
    public void FocusSearch()=>focusSearch=true;
    public bool IsCollapsed(string id)=>collapsed.Contains(id);
    public void SetCollapsed(string id,bool value){if(value)collapsed.Add(id);else collapsed.Remove(id);}
    private static float Scale=>ImGui.GetTextLineHeight()/16f;
    private static void Gap(float height)=>ImGui.Dummy(new Vector2(0,height*Scale));
    private void Search()
    {
        if(focusSearch){ImGui.SetKeyboardFocusHere();focusSearch=false;}
        Controls.Search("##row-search","Filter settings...",ref query,160);
    }
    private void Header(Group group,bool searching)
    {
        var pos=ImGui.GetCursorScreenPos();
        float width=ImGui.GetContentRegionAvail().X;
        ImGui.PushStyleColor(ImGuiCol.Button,Vector4.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize,0);
        try
        {
            if(ImGui.Button("###group",new Vector2(width,27*Scale))&&!searching)
                SetCollapsed(group.Id,!IsCollapsed(group.Id));
        }
        finally {ImGui.PopStyleColor();ImGui.PopStyleVar();}
        var dl=ImGui.GetWindowDrawList();
        float font=ImGui.GetFontSize()*.96f;
        string title=group.Name.ToUpperInvariant();
        string status=group.Status.ToUpperInvariant();
        float right=pos.X+width-12*Scale;
        if(!searching)
        {
            var tip=new Vector2(right-3*Scale,pos.Y+12*Scale);
            var arrow=IsCollapsed(group.Id)?new Vector2(3,0):new Vector2(0,3);
            var side=new Vector2(-arrow.Y,arrow.X);
            dl.AddTriangleFilled(tip+arrow*Scale,tip+side*Scale,tip-side*Scale,0xffc6beb8);
            right-=12*Scale;
        }
        float statusX=right-ImGui.CalcTextSize(status).X*.96f;
        var labelPos=pos+new Vector2(12,5)*Scale;
        float available=Math.Max(0,statusX-labelPos.X-8*Scale);
        if(ImGui.CalcTextSize(title).X*.96f>available){
            if(ImGui.IsItemHovered()||ImGui.IsItemFocused())ImGui.SetTooltip(group.Name);
            while(title.Length>0&&ImGui.CalcTextSize(title+"…").X*.96f>available)title=title[..^1];
            title+="…";
        }
        dl.PushClipRect(labelPos,new Vector2(labelPos.X+available,pos.Y+27*Scale),true);
        dl.AddText(ImGui.GetFont(),font,labelPos,0xffc6beb8,title);
        dl.PopClipRect();
        dl.AddText(ImGui.GetFont(),font,new Vector2(statusX,pos.Y+5*Scale),0xffc6beb8,status);
    }
    private string Topics(Group group,string current)
    {
        float edge=ImGui.GetCursorScreenPos().X+ImGui.GetContentRegionAvail().X;
        for(int i=0;i<group.Topics.Length;i++)
        {
            string name=group.Topics[i].Name;
            float width=ImGui.CalcTextSize(name).X+2*ImGui.GetStyle().FramePadding.X+5*Scale;
            if(i>0&&ImGui.GetItemRectMax().X+6*Scale+width<=edge)ImGui.SameLine(0,6*Scale);
            bool active=current==name;
            if(active){ImGui.PushStyleColor(ImGuiCol.Button,0x40a4aa48);ImGui.PushStyleColor(ImGuiCol.Border,0x8cc5cf70);}
            try{if(ImGui.Button(name,new Vector2(width,31*Scale)))selected[group.Id]=current=name;}
            finally{if(active)ImGui.PopStyleColor(2);}
        }
        return current;
    }
    public void Draw(string id,IReadOnlyList<Group> groups,bool searchable=true,Action? toolbar=null,bool inlineTopics=false)
    {
        ImGui.PushID(id);
        try
        {
            if(searchable)Search();
            string[] words=searchable?query.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries):[];
            bool searching=words.Length>0;
            if(!searching)toolbar?.Invoke();
            int count=0;
            string appliedQuery=searching?query:"";
            bool visible=ScrollRegion.Begin("rows");
            try
            {
                ScrollPosition=ImGui.GetScrollY();
                if(appliedQuery!=lastQuery){
                    if(lastQuery.Length==0)browseScroll=ImGui.GetScrollY();
                    ImGui.SetScrollY(searching?0:browseScroll);
                    lastQuery=appliedQuery;
                }
                if(visible)foreach(var group in groups)
                {
                    if(group.Topics.Length==0 && group.Body==null)continue;
                    var current=selected.GetValueOrDefault(group.Id,group.DefaultTopic??group.Topics.FirstOrDefault()?.Name??"");
                    if(group.Topics.Length>0&&!group.Topics.Any(t=>t.Name==current))current=group.Topics[0].Name;
                    bool Matches(Topic t,Setting r)=>(r.Visible?.Invoke()??true)&&words.All(w=>
                        $"{group.Name} {t.Name} {r.Label} {r.Keywords}".Contains(w,StringComparison.OrdinalIgnoreCase));
                    if(searching&&!group.Topics.Any(t=>t.Rows.Any(r=>Matches(t,r))))continue;
                    ImGui.PushID(group.Id);
                    try
                    {
                        Gap(6);Header(group,searching);
                        if(scrollTarget==group.Id){ImGui.SetScrollHereY(0);scrollTarget=null;}
                        if(!searching)
                        {
                            if(group.Summary.Length>0){ImGui.TextDisabled(group.Summary);Gap(4);}
                            if(IsCollapsed(group.Id))continue;
                            group.Body?.Invoke();
                            if(!inlineTopics && group.Topics.Length>1)current=Topics(group,current);
                        }
                        Gap(1);
                        var rows=group.Topics.Where(t=>inlineTopics||searching||t.Name==current)
                            .SelectMany(t=>t.Rows.Where(r=>Matches(t,r)).Select(r=>(t.Name,Row:r))).ToArray();
                        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing,new Vector2(ImGui.GetStyle().ItemSpacing.X,0));
                        try
                        {
                            for(int i=0;i<rows.Length;i++)
                            {
                                var (topic,r)=rows[i];
                                bool first=i==0 || (inlineTopics && rows[i-1].Name!=topic);
                                bool last=i==rows.Length-1 || (inlineTopics && rows[i+1].Name!=topic);
                                if(inlineTopics && first && group.Topics.Length>1){
                                    Gap(6);ImGui.TextDisabled(topic);Gap(3);
                                }
                                string? reason=r.DisabledReason?.Invoke();
                                var detail=string.Join(" ",new[]{r.Detail,reason}.Where(x=>!string.IsNullOrEmpty(x)));
                                ImGui.PushID(topic);
                                try{Controls.Row(r.Id,r.Label,detail,()=>{
                                    ImGui.BeginDisabled(!string.IsNullOrEmpty(reason));
                                    try{r.Draw();}finally{ImGui.EndDisabled();}
                                },r.Layout,first,last);
                                    if(r.ExpandedContent!=null){
                                        ImGui.PushID(r.Id);ImGui.BeginDisabled(!string.IsNullOrEmpty(reason));
                                        try{r.ExpandedContent();}finally{ImGui.EndDisabled();ImGui.PopID();}
                                    }
                                }
                                finally{ImGui.PopID();}
                            }
                        }
                        finally{ImGui.PopStyleVar();}
                        count+=rows.Length;Gap(5);
                    }
                    finally{ImGui.PopID();}
                }
                LastRowCount=count;
                if(visible&&searching)ImGui.TextDisabled(count==0?"No matching settings. Clear search to see all rows.":$"{count} matching setting(s)");
            }
            finally{ScrollRegion.End();}
        }
        finally{ImGui.PopID();}
    }
}
