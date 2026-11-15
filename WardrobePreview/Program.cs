using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using AetherglassUI;
using AetherglassUI.Preview;
using Aetherloom;
using Dalamud.Bindings.ImGui;

internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        using var form=new RenderForm(args.FirstOrDefault(),args.ElementAtOrDefault(1));
        Application.Run(form);
    }
}
internal sealed class RenderForm(string? scene,string? output):Form
{
    private GlSurface? surface;
    private WardrobeLayout? layout;
    private int frame;
    private WardrobeFixture? fixture;
    private readonly System.Windows.Forms.Timer timer=new(){Interval=25};
    protected override unsafe void OnLoad(EventArgs e)
    {
        base.OnLoad(e); Text="AetherglassUI · Aetherloom";ClientSize=new(int.TryParse(Environment.GetEnvironmentVariable("WARDROBE_PREVIEW_WIDTH"),out var width)?width:1250,760);
        SetStyle(ControlStyles.UserPaint|ControlStyles.Opaque|ControlStyles.AllPaintingInWmPaint,true);
        ImGui.CreateContext();var io=ImGui.GetIO();io.IniFilename=null;
        io.Fonts.AddFontFromFileTTF(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"segoeui.ttf"),16);
        surface=new GlSurface(Handle);
        fixture=new WardrobeFixture(LoadReference);
        if(scene=="scroll-library")fixture.FillLibrary();
        if(scene=="dye-neutral"){fixture.SetDye("Body",0,0);fixture.FavoriteDyes.Clear();}
        layout=new(fixture);fixture.Layout=layout;layout.ConfigureScene(scene);
        timer.Tick+=(_,_)=>Invalidate();timer.Start();
    }
    private ImTextureID LoadReference(string name)
    {
        using var stream=typeof(RenderForm).Assembly.GetManifestResourceStream(name+".png")!;
        using var bmp=new Bitmap(stream);var rect=new Rectangle(0,0,bmp.Width,bmp.Height);
        var bits=bmp.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try{var pixels=new byte[bmp.Width*bmp.Height*4];Marshal.Copy(bits.Scan0,pixels,0,pixels.Length);return surface!.Upload(bmp.Width,bmp.Height,pixels,true);}
        finally{bmp.UnlockBits(bits);}
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        if(surface==null||layout==null)return;
        var io=ImGui.GetIO();io.DisplaySize=new(ClientSize.Width,ClientSize.Height);io.DeltaTime=.025f;
        if(scene=="scroll-library" && frame is >=2 and <=8)
        {
            io.AddMousePosEvent(118,405);
            if(frame>=3)io.AddMouseWheelEvent(0,-3);
        }
        if(scene=="scroll-library" && frame is >=11 and <=13)
        {
            io.AddMousePosEvent(235,100);
            if(frame>=12)io.AddMouseButtonEvent(0,frame==12);
        }
        if(scene=="job-select" && frame==4)foreach(char c in "brd")io.AddInputCharacter(c);
        if((scene is "gender-select" or "job-select") && frame is 5 or 6 or 7)
        {
            io.AddMousePosEvent(scene=="gender-select"?380:706,scene=="gender-select"?292:320);
            if(frame>=6)io.AddMouseButtonEvent(0,frame==6);
        }
        if(scene=="weapon-filter" && frame is 3 or 4 or 5)
        {
            io.AddMousePosEvent(510,272);
            if(frame>=4)io.AddMouseButtonEvent(0,frame==4);
        }
        if((scene is "filter-clear" or "tag-select") && frame is 3 or 4 or 5)
        {
            io.AddMousePosEvent(scene=="filter-clear"?277:480,scene=="filter-clear"?471:389);
            if(frame>=4)io.AddMouseButtonEvent(0,frame==4);
        }
        if((scene is "folder-menu" or "design-menu" or "design-clone") && frame is 2 or 3)
        {
            io.AddMousePosEvent(84,scene=="folder-menu"?178:244);
            io.AddMouseButtonEvent(1,frame==2);
        }
        if(scene=="design-clone" && frame is 5 or 6 or 7)
        {
            io.AddMousePosEvent(132,287);
            if(frame>=6)io.AddMouseButtonEvent(0,frame==6);
        }
        if(scene=="ownership-bypass" && frame is 2 or 3 or 4)
        {
            io.AddMousePosEvent(870,67);
            if(frame>=3)io.AddMouseButtonEvent(0,frame==3);
        }
        if(scene=="folder-drag")
        {
            if(frame is 1 or 2)io.AddMousePosEvent(85,211);
            if(frame==2)io.AddMouseButtonEvent(0,true);
            if(frame==3)io.AddMousePosEvent(105,210);
            if(frame is 4 or 5)io.AddMousePosEvent(80,145);
            if(frame==6)io.AddMouseButtonEvent(0,false);
        }
        if((scene is "clear-gear" or "clear-accessory") && frame is 5 or 6 or 7)
        {
            io.AddMousePosEvent(430,scene=="clear-gear"?473:357);
            if(frame>=6)io.AddMouseButtonEvent(0,frame==6);
        }
        ImGui.NewFrame();ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(io.DisplaySize);
        WindowShell.PushHostStyle();ImGui.Begin("Aetherloom",WindowShell.HostFlags|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove);WindowShell.PopHostStyle();
        if(frame is 1 or 2)layout.Show(scene switch{"gender-select" or "job-select" or "designed-for" or "intended-for" or "details" or "filters" or "tags" or "filter-clear" or "tag-select"=>"Details","hair" or "paint" or "colors" or "appearance"=>"Appearance","accessories" or "earrings" or "clear-accessory"=>"Accessories","assignments" or "jobs" or "zones"=>"Assignments",_=>"Gear"},
            scene switch{"gender-select" or "designed-for"=>WardrobeMetadata.Gender,"job-select" or "intended-for"=>WardrobeMetadata.Intended,"import"=>"import","tags" or "tag-select"=>"tag-Style","filters" or "filter-clear" or "weapon-filter"=>"filters","jobs"=>"Jobs","zones"=>"Zones","dye" or "dye-neutral"=>"Body-dye0","colors"=>"HairColor","items" or "clear-gear"=>"Body","earrings" or "clear-accessory"=>"Ears","hair"=>"Hairstyle","paint"=>"FacePaint",_=>null});
        layout.Draw(()=>BeginInvoke(Close));ImGui.End();ImGui.Render();surface.Paint(ClientSize.Width,ClientSize.Height);
        if(++frame==(scene=="scroll-library"?17:scene is "design-clone" or "folder-drag" or "clear-gear" or "clear-accessory" or "weapon-filter" or "gender-select" or "job-select"?9:6) && output!=null){
            if(scene=="gender-select" && !fixture!.Tags.Contains("gender:Male"))
                throw new InvalidOperationException("Selecting Male did not update the design.");
            if(scene=="job-select" && !fixture!.Tags.Contains("intended:Bard"))
                throw new InvalidOperationException("Searching BRD and choosing Bard did not update the design.");
            if(scene=="weapon-filter" && fixture!.WeaponTermRequests==0)
                throw new InvalidOperationException("The weapon job dropdown did not query configured weapons.");
            if(scene=="filter-clear" && !layout.FilterClearPassed)
                throw new InvalidOperationException("Clearing filters must preserve the search text.");
            if(scene=="tag-select" && !fixture!.Tags.Contains("style:Vintage"))
                throw new InvalidOperationException("Selecting a style suggestion must save its category.");
            if(scene=="ownership-bypass" && !fixture!.BypassOwnership)
                throw new InvalidOperationException("Design title switch did not enable the ownership bypass.");
            if(scene=="design-clone" && (fixture!.Title!="Highland Scout (copy)" ||
                !fixture.Designs.Any(d=>d.Name=="Highland Scout" && d.Id!=fixture.Selected) ||
                fixture.Assigned("Jobs").Count!=0))
                throw new InvalidOperationException("Cloning must select an independent, unassigned copy.");
            if(scene=="folder-drag" && !fixture!.Folders.Contains("[AL] Winter"))
                throw new InvalidOperationException("Dropping Winter onto the root did not promote it.");
            if(scene is "clear-gear" or "clear-accessory")
            {
                bool accessory=scene=="clear-accessory";
                var slot=fixture!.Slots(accessory).Single(s=>s.Key==(accessory?"Ears":"Body"));
                if(slot.Item.Id!=0 || slot.First!=0 || slot.Second!=0 || !slot.Apply)
                    throw new InvalidOperationException("Empty slot should clear the item and both dyes and enable application.");
            }
            surface.Capture(output,ClientSize.Width,ClientSize.Height);BeginInvoke(Close);}
    }
    protected override void OnMouseMove(MouseEventArgs e){if(surface!=null)ImGui.GetIO().AddMousePosEvent(e.X,e.Y);}
    protected override void OnMouseDown(MouseEventArgs e){if(surface!=null)ImGui.GetIO().AddMouseButtonEvent(e.Button==MouseButtons.Right?1:0,true);}
    protected override void OnMouseUp(MouseEventArgs e){if(surface!=null)ImGui.GetIO().AddMouseButtonEvent(e.Button==MouseButtons.Right?1:0,false);}
    protected override void OnMouseWheel(MouseEventArgs e){if(surface!=null)ImGui.GetIO().AddMouseWheelEvent(0,e.Delta/120f);}
    protected override void OnKeyPress(KeyPressEventArgs e){if(surface!=null&&!char.IsControl(e.KeyChar))ImGui.GetIO().AddInputCharacter(e.KeyChar);}
    protected override void OnFormClosed(FormClosedEventArgs e){timer.Dispose();surface?.Dispose();ImGui.DestroyContext();base.OnFormClosed(e);}
}

namespace Aetherloom
{
    internal sealed partial class WardrobeLayout
    {
        internal void ConfigureScene(string? scene)
        {
            if(scene is "tags" or "tag-select")newTag="Vi";
            if(scene=="filter-clear"){filter="Highland";onlyAvailable=true;}
            if(scene is not ("filters" or "filter-clear"))return;
            filters[WardrobeMetadata.Intended].Add("Ranged DPS");
            filters["Style"].Add("Casual");
        }
        internal bool FilterClearPassed=>FilterCount==0 && filter=="Highland" && !onlyAvailable;
    }
}
