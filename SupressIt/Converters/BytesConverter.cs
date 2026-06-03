using System; using System.Globalization; using System.Windows.Data;
namespace SupressIt.Converters
{
    public class BytesConverter : IValueConverter
    {
        public object Convert(object v,Type t,object p,CultureInfo c){
            long b=System.Convert.ToInt64(v);
            if(b==0)return"—";if(b<1024)return$"{b} B/s";
            if(b<1_048_576)return$"{b/1024.0:F1} KB/s";return$"{b/1_048_576.0:F1} MB/s";}
        public object ConvertBack(object v,Type t,object p,CultureInfo c)=>throw new NotImplementedException();
    }
}
