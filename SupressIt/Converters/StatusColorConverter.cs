using System; using System.Globalization; using System.Windows.Data; using System.Windows.Media;
namespace SupressIt.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object v,Type t,object p,CultureInfo c)=>
            v?.ToString()=="Running"?new SolidColorBrush(Color.FromRgb(0x40,0xC0,0x60)):new SolidColorBrush(Color.FromRgb(0x40,0x40,0x50));
        public object ConvertBack(object v,Type t,object p,CultureInfo c)=>throw new NotImplementedException();
    }
}
