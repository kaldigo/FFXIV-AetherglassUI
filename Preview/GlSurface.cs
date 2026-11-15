using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;

namespace AetherglassUI.Preview;

// Windows' built-in OpenGL 1.1 is sufficient for this single-window, font-atlas preview.
internal sealed unsafe class GlSurface : IDisposable
{
    private readonly nint window, dc, context;
    private readonly uint atlas;
    public ImTextureID LoadMark()
    {
        using var stream=typeof(GlSurface).Assembly.GetManifestResourceStream("StewardMark.png")!;
        using var bitmap=new Bitmap(stream);
        byte[] rgba=new byte[bitmap.Width*bitmap.Height*4];
        int i=0;
        for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++)
        {
            var c=bitmap.GetPixel(x,y);rgba[i++]=c.R;rgba[i++]=c.G;rgba[i++]=c.B;rgba[i++]=c.A;
        }
        return Upload(bitmap.Width,bitmap.Height,rgba,false);
    }

    internal ImTextureID Upload(int width,int height,byte[] rgba,bool bgra)
    {
        if(rgba.Length!=checked(width*height*4))throw new InvalidDataException("Incomplete preview texture");
        glGenTextures(1,out uint texture);
        glBindTexture(0x0de1,texture);
        glTexParameteri(0x0de1,0x2800,0x2601);glTexParameteri(0x0de1,0x2801,0x2601);
        fixed(byte* data=rgba)glTexImage2D(0x0de1,0,4,width,height,0,bgra?0x80e1u:0x1908u,0x1401,data);
        return new ImTextureID((ulong)texture);
    }

    public void Capture(string path,int width,int height)
    {
        using var bitmap=new Bitmap(width,height,System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        var pixels=bitmap.LockBits(new Rectangle(0,0,width,height),System.Drawing.Imaging.ImageLockMode.WriteOnly,bitmap.PixelFormat);
        try { glReadBuffer(0x0405);glReadPixels(0,0,width,height,0x80e1,0x1401,(void*)pixels.Scan0); }
        finally { bitmap.UnlockBits(pixels); }
        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
        bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);
    }
    public GlSurface(nint window)
    {
        this.window=window;
        dc=GetDC(window);
        var format=new PixelFormat { Size=(ushort)sizeof(PixelFormat), Version=1, Flags=0x25, ColorBits=32, AlphaBits=8 };
        int index=ChoosePixelFormat(dc,ref format);
        context=index!=0 && SetPixelFormat(dc,index,ref format) ? wglCreateContext(dc) : 0;
        if (context==0 || !wglMakeCurrent(dc,context))
        {
            Dispose();
            throw new InvalidOperationException("Could not initialize the Windows OpenGL preview surface.");
        }
        byte* pixels; int width,height;
        var fonts=ImGui.GetIO().Fonts;
        fonts.GetTexDataAsRGBA32(0,&pixels,&width,&height);
        glGenTextures(1,out atlas);
        glBindTexture(0x0DE1,atlas);
        glTexParameteri(0x0DE1,0x2801,0x2601);
        glTexParameteri(0x0DE1,0x2800,0x2601);
        glTexImage2D(0x0DE1,0,4,width,height,0,0x1908,0x1401,pixels);
        fonts.SetTexID(0,new ImTextureID((ulong)atlas));
    }
    public void Paint(int width,int height)
    {
        glViewport(0,0,width,height);
        glMatrixMode(0x1701); glLoadIdentity(); glOrtho(0,width,height,0,-1,1);
        glMatrixMode(0x1700); glLoadIdentity();
        glClear(0x4000); glEnable(0x0BE2); glBlendFunc(0x0302,0x0303);
        glEnable(0x0DE1); glEnable(0x0C11);
        glEnableClientState(0x8074); glEnableClientState(0x8076); glEnableClientState(0x8078);
        var data=ImGui.GetDrawData();
        for(int i=0;i<data.CmdListsCount;i++)
        {
            var list=data.CmdLists[i];
            for(int j=0;j<list->CmdBuffer.Size;j++)
            {
                var cmd=list->CmdBuffer.Data[j];
                var v=list->VtxBuffer.Data+cmd.VtxOffset;
                glVertexPointer(2,0x1406,sizeof(ImDrawVert),&v->Pos);
                glTexCoordPointer(2,0x1406,sizeof(ImDrawVert),&v->Uv);
                glColorPointer(4,0x1401,sizeof(ImDrawVert),&v->Col);
                var r=cmd.ClipRect;
                glScissor((int)r.X,height-(int)r.W,Math.Max(0,(int)r.Z-(int)r.X),Math.Max(0,(int)r.W-(int)r.Y));
                glBindTexture(0x0DE1,(uint)cmd.TextureId.Handle);
                glDrawElements(4,(int)cmd.ElemCount,0x1403,list->IdxBuffer.Data+cmd.IdxOffset);
            }
        }
        glDisable(0x0C11); SwapBuffers(dc);
    }
    public void Dispose()
    {
        if(context!=0){wglMakeCurrent(0,0);wglDeleteContext(context);}
        if(dc!=0)ReleaseDC(window,dc);
    }
    [StructLayout(LayoutKind.Sequential,Size=40)]
    private struct PixelFormat { public ushort Size,Version; public uint Flags; public byte Type,ColorBits,RedBits,RedShift,GreenBits,GreenShift,BlueBits,BlueShift,AlphaBits; }
    [DllImport("user32")] private static extern nint GetDC(nint h);
    [DllImport("user32")] private static extern int ReleaseDC(nint h,nint d);
    [DllImport("gdi32")] private static extern int ChoosePixelFormat(nint d,ref PixelFormat f);
    [DllImport("gdi32")] private static extern bool SetPixelFormat(nint d,int i,ref PixelFormat f);
    [DllImport("gdi32")] private static extern bool SwapBuffers(nint d);
    private const string GL="opengl32";
    [DllImport(GL)] private static extern void glReadBuffer(uint buffer);
    [DllImport(GL)] private static extern void glReadPixels(int x,int y,int w,int h,uint format,uint type,void* data);
    [DllImport(GL)] private static extern nint wglCreateContext(nint d);
    [DllImport(GL)] private static extern bool wglMakeCurrent(nint d,nint c);
    [DllImport(GL)] private static extern bool wglDeleteContext(nint c);
    [DllImport(GL)] private static extern void glViewport(int x,int y,int w,int h);
    [DllImport(GL)] private static extern void glMatrixMode(uint mode);
    [DllImport(GL)] private static extern void glLoadIdentity();
    [DllImport(GL)] private static extern void glOrtho(double l,double r,double b,double t,double n,double f);
    [DllImport(GL)] private static extern void glGenTextures(int count,out uint id);
    [DllImport(GL)] private static extern void glBindTexture(uint t,uint id);
    [DllImport(GL)] private static extern void glTexParameteri(uint t,uint p,int v);
    [DllImport(GL)] private static extern void glTexImage2D(uint t,int level,int format,int w,int h,int border,uint source,uint type,void* p);
    [DllImport(GL)] private static extern void glClear(uint flags);
    [DllImport(GL)] private static extern void glEnable(uint flags);
    [DllImport(GL)] private static extern void glDisable(uint flags);
    [DllImport(GL)] private static extern void glBlendFunc(uint src,uint dst);
    [DllImport(GL)] private static extern void glEnableClientState(uint state);
    [DllImport(GL)] private static extern void glVertexPointer(int n,uint type,int stride,void* data);
    [DllImport(GL)] private static extern void glTexCoordPointer(int n,uint type,int stride,void* data);
    [DllImport(GL)] private static extern void glColorPointer(int n,uint type,int stride,void* data);
    [DllImport(GL)] private static extern void glScissor(int x,int y,int w,int h);
    [DllImport(GL)] private static extern void glDrawElements(uint mode,int count,uint type,void* data);
}
