using System.Windows.Media.Imaging;
namespace PCControl;
public static class BrandAssets
{
    public static BitmapImage Logo {get;}=LoadLogo();
    static BitmapImage LoadLogo()
    {
        var image=new BitmapImage();image.BeginInit();
        image.UriSource=new Uri("pack://application:,,,/PCControl;component/assets/pc-control.png");
        image.DecodePixelWidth=128;image.CacheOption=BitmapCacheOption.OnLoad;
        image.EndInit();image.Freeze();return image;
    }
}
