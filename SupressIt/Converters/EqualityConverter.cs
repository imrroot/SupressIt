using System; using System.Globalization; using System.Windows.Data;
namespace SupressIt.Converters
{
    public class EqualityConverter : IValueConverter
    {
        public object Convert(object v,Type t,object p,CultureInfo c)=>v?.ToString()==p?.ToString();
        public object ConvertBack(object v,Type t,object p,CultureInfo c)=>
            v is bool b&&b?(object)p?.ToString():System.Windows.DependencyProperty.UnsetValue;
    }
}
