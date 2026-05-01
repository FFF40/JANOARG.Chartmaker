using System.Xml.Serialization;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Shared.Data.Chartmaker;

namespace JANOARG.Chartmaker.Data
{

    [XmlRoot("ItemList")]
    [XmlInclude(typeof(RecentSong))]
    public class ClientSerializeProxyList : SerializeProxyList
    {
    }

    public class Storage : Storage<ClientSerializeProxyList>
    {
        public Storage(string path) : base(path) { }
    }
}