using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
namespace PCControl;
public sealed class OverlayWindow : Window
{
    readonly Grid grid=new();
    readonly Border surface=new(){CornerRadius=new(10),Padding=new(12)};
    readonly List<(Tile tile,TextBlock value)> labels=[];
    public OverlayWindow()
    {
        Title="PC Control · Overlay";WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Topmost=true;ResizeMode=ResizeMode.NoResize;SizeToContent=SizeToContent.Height;Focusable=false;
        surface.Child=grid;Content=surface;
        SourceInitialized+=(_,_)=>{var h=new WindowInteropHelper(this).Handle;SetWindowLongPtr(h,-20,new IntPtr(GetWindowLongPtr(h,-20).ToInt64()|0x20|0x80000|0x08000000|0x80));HwndSource.FromHwnd(h).AddHook(Hook);};
    }
    public void Configure(Profile p)
    {
        Width=Math.Min(p.OverlayWidth,SystemParameters.VirtualScreenWidth);
        Left=Math.Clamp(p.OverlayX,SystemParameters.VirtualScreenLeft,SystemParameters.VirtualScreenLeft+SystemParameters.VirtualScreenWidth-Width);
        Top=Math.Clamp(p.OverlayY,SystemParameters.VirtualScreenTop,SystemParameters.VirtualScreenTop+SystemParameters.VirtualScreenHeight-100);
        surface.Background=p.OverlayBackground?new SolidColorBrush(Color.FromArgb((byte)(255*p.OverlayOpacity),16,18,25)):Brushes.Transparent;
        grid.Children.Clear();grid.RowDefinitions.Clear();grid.ColumnDefinitions.Clear();labels.Clear();
        for(int i=0;i<p.OverlayColumns;i++)grid.ColumnDefinitions.Add(new());
        var placements=GridLayout.Pack(p.Overlay,p.OverlayColumns);int rows=placements.Count==0?1:placements.Max(x=>x.row+x.height);for(int i=0;i<rows;i++)grid.RowDefinitions.Add(new(){MinHeight=p.OverlayFont*3.8});
        for(int i=0;i<p.Overlay.Count;i++){var t=p.Overlay[i];var pos=placements[i];var panel=new StackPanel{Margin=new(6)};panel.Children.Add(Ui.Text(t.Metric,Math.Max(10,p.OverlayFont*.65),"#CBD0DF"));var v=Ui.Text("—",p.OverlayFont,t.Color=="#51E3D5"?p.OverlayColor:t.Color);panel.Children.Add(v);Grid.SetRow(panel,pos.row);Grid.SetColumn(panel,pos.col);Grid.SetRowSpan(panel,pos.height);Grid.SetColumnSpan(panel,pos.width);grid.Children.Add(panel);labels.Add((t,v));}
    }
    public void Update(IReadOnlyDictionary<string,Metric> metrics)
    {
        foreach(var (tile,value) in labels){value.Text=metrics.TryGetValue(tile.Metric,out var m)?m.Display:"No disponible";if(((StackPanel)value.Parent).Children[0] is TextBlock title)title.Text=m?.Name??tile.Metric;}
    }
    static IntPtr Hook(IntPtr h,int msg,IntPtr w,IntPtr l,ref bool handled){if(msg==0x84){handled=true;return new(-1);}return IntPtr.Zero;}
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr h,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr h,int index,IntPtr value);
}
