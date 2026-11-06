using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace PCControl;
public static class Ui
{
    public static Brush Brush(string value) {try{return (Brush)new BrushConverter().ConvertFromString(value)!;}catch{return Brushes.Turquoise;}}
    public static TextBlock Text(string text,double size=14,string color="#A7AEC5")=>new(){Text=text,FontSize=size,Foreground=Brush(color),Margin=new(0,4,0,8)};
    public static Button Button(string text,Action action) {var b=new Button{Content=text};b.Click+=(_,_)=>action();return b;}
    public static StackPanel Row(params UIElement[] elements) { var p=new StackPanel{Orientation=Orientation.Horizontal};foreach(var e in elements)p.Children.Add(e);return p; }
    public static Border Box(UIElement child) {var b=new Border{CornerRadius=new(12),Padding=new(20),Margin=new(0,0,0,16),Child=child};b.SetResourceReference(Border.BackgroundProperty,"SurfaceBackground");return b;}
    public static TextBox Input(string value)=>new(){Text=value,MinWidth=150};
    public static ComboBox Select(IEnumerable<string> values,string? selected=null) { var c=new ComboBox();foreach(var v in values)c.Items.Add(v);c.SelectedItem=selected;if(c.SelectedIndex<0&&c.Items.Count>0)c.SelectedIndex=0;return c; }
    public static Window Dialog(Window owner,string title,StackPanel content,double width=490)
    {
        var w=new Window{Title=title,Owner=owner,Icon=BrandAssets.Logo,Width=width,SizeToContent=SizeToContent.Height,MaxHeight=850,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize};
        content.Margin=new(24);w.Content=new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};return w;
    }
    public static string? Ask(Window owner,string title,string initial)
    {
        var p=new StackPanel();p.Children.Add(Text(title,20,"#FFFFFF"));var t=Input(initial);p.Children.Add(t);var w=Dialog(owner,title,p);string? value=null;
        p.Children.Add(Button("Guardar",()=>{if(string.IsNullOrWhiteSpace(t.Text)){t.Focus();return;}value=t.Text.Trim();w.DialogResult=true;}));w.ShowDialog();return value;
    }
}
public sealed class Sparkline : FrameworkElement
{
    public IReadOnlyList<double?> Values {get;set;}=[];
    public Brush Line {get;set;}=Brushes.Turquoise;
    public string ChartStyle {get;set;}="Línea";
    public bool Gaming {get;set;}
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);if(ActualWidth<1||ActualHeight<1||Values.Count<2)return;
        var data=Values.Where(x=>x.HasValue).Select(x=>x!.Value).ToArray();if(data.Length<1)return;
        var min=Math.Min(0,data.Min());var max=Math.Max(min+1,data.Max()*1.08);
        if(Gaming) {
            var gridPen=new Pen(Ui.Brush("#253546"),.7);
            for(int j=1;j<4;j++)dc.DrawLine(gridPen,new(0,ActualHeight*j/4),new(ActualWidth,ActualHeight*j/4));
            for(int j=1;j<8;j++)dc.DrawLine(gridPen,new(ActualWidth*j/8,0),new(ActualWidth*j/8,ActualHeight));
        }
        dc.DrawLine(new Pen(Ui.Brush("#30364B"),1),new(0,ActualHeight-1),new(ActualWidth,ActualHeight-1));
        Point? prev=null;
        for(int i=0;i<Values.Count;i++) {if(Values[i] is not double v){prev=null;continue;}var pt=new Point(i*ActualWidth/Math.Max(1,Values.Count-1),ActualHeight-(v-min)/(max-min)*ActualHeight);
            if(ChartStyle=="Barras")dc.DrawLine(new Pen(Line,Math.Max(1,ActualWidth/Math.Max(60,Values.Count)-1)),new(pt.X,ActualHeight),pt);
            else if(prev is Point q) {if(ChartStyle=="Área"||Gaming){var geo=new StreamGeometry();using(var c=geo.Open()){c.BeginFigure(new(q.X,ActualHeight),true,true);c.LineTo(q,true,false);c.LineTo(pt,true,false);c.LineTo(new(pt.X,ActualHeight),true,false);}var b=Line.Clone();b.Opacity=Gaming?.23:.15;dc.DrawGeometry(b,null,geo);}if(Gaming){var halo=Line.Clone();halo.Opacity=.14;dc.DrawLine(new Pen(halo,7),q,pt);}dc.DrawLine(new Pen(Line,Gaming?2.5:2),q,pt);}prev=pt; }
        if(Gaming&&prev is Point end){dc.DrawEllipse(Line,null,end,3.5,3.5);}
    }
}
public sealed class GamingBackdrop : FrameworkElement
{
    public bool Enabled {get;set;}
    protected override void OnRender(DrawingContext dc)
    {
        if(!Enabled)return;
        var pen=new Pen(Ui.Brush("#132A3546"),1);
        for(double x=0;x<ActualWidth;x+=48)dc.DrawLine(pen,new(x,0),new(x,ActualHeight));
        for(double y=0;y<ActualHeight;y+=48)dc.DrawLine(pen,new(0,y),new(ActualWidth,y));
        var rail=new Pen(Ui.Brush("#33FF3FA4"),2);
        dc.DrawLine(rail,new(0,130),new(ActualWidth,130));
        dc.DrawLine(new Pen(Ui.Brush("#33C1FF38"),3),new(28,130),new(Math.Min(250,ActualWidth),130));
    }
}
public static class GridLayout
{
    // Stable first-fit packing; cells are occupied once, including multi-row cards.
    public static List<(int row,int col,int width,int height)> Pack(IReadOnlyList<Tile> tiles,int columns)
    {
        var occupied=new HashSet<(int,int)>();var result=new List<(int,int,int,int)>();
        foreach(var t in tiles){int width=Math.Clamp(t.Width,1,columns),height=Math.Clamp(t.Height,1,3);int r=0,c=0;
            for(;;){bool fits=true;for(int y=r;y<r+height;y++)for(int x=c;x<c+width;x++)if(occupied.Contains((y,x)))fits=false;if(fits)break;c++;if(c+width>columns){c=0;r++;}}
            for(int y=r;y<r+height;y++)for(int x=c;x<c+width;x++)occupied.Add((y,x));result.Add((r,c,width,height));}
        return result;
    }
}
