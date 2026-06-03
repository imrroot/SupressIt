using System; using System.Globalization; using System.Windows.Data; using System.Windows.Media;
namespace SupressIt.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object v,Type t,object p,CultureInfo c)=>
            v is bool b&&b?new SolidColorBrush(Color.FromRgb(0x60,0x60,0xFF)):new SolidColorBrush(Color.FromRgb(0x30,0xA0,0x60));
        public object ConvertBack(object v,Type t,object p,CultureInfo c)=>throw new NotImplementedException();
    }
}
