using System; using System.Globalization; using System.Windows.Data;
using SupressIt.Helpers;
namespace SupressIt.Converters
{
    public class PathToIconConverter : IValueConverter
    {
        public object Convert(object v,Type t,object p,CultureInfo c)=>IconHelper.GetIcon(v as string);
        public object ConvertBack(object v,Type t,object p,CultureInfo c)=>throw new NotImplementedException();
    }
}
