using System; using System.Collections.Concurrent; using System.Drawing;
using System.Windows; using System.Windows.Interop; using System.Windows.Media; using System.Windows.Media.Imaging;
namespace SupressIt.Helpers
{
    public static class IconHelper
    {
        static readonly ConcurrentDictionary<string,ImageSource> _cache=new();
        static ImageSource _def;
        public static ImageSource GetIcon(string path)
        {
            if(string.IsNullOrEmpty(path))return Default();
            return _cache.GetOrAdd(path,p=>{
                try{using var ic=Icon.ExtractAssociatedIcon(p);if(ic==null)return Default();
                    return Imaging.CreateBitmapSourceFromHIcon(ic.Handle,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());}
                catch{return Default();}});
        }
        static ImageSource Default()=>_def??=new WriteableBitmap(32,32,96,96,PixelFormats.Pbgra32,null);
    }
}
