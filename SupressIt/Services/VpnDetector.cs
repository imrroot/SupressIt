using System; using System.Diagnostics; using System.Linq; using System.Net.NetworkInformation;
namespace SupressIt.Services
{
    public class VpnStatus{public bool IsActive{get;init;}public string Name{get;init;}}
    public class VpnDetector : IVpnDetector
    {
        static readonly string[] ProcKeys={"openvpn","nordvpn","expressvpn","protonvpn","windscribe",
            "mullvadvpn","cyberghost","ipvanish","surfshark","wireguard","vpnclient","vpnui","forticlient","anyconnect"};
        static readonly string[] NicKeys={"vpn","tun","tap","wintun","wireguard","nordvpn","expressvpn","protonvpn","mullvad","surfshark"};
        public VpnStatus Check(){
            try{foreach(var p in Process.GetProcesses())try{var n=p.ProcessName.ToLower();
                if(ProcKeys.Any(k=>n.Contains(k)))return new VpnStatus{IsActive=true,Name=p.ProcessName};}catch{}}catch{}
            try{foreach(var n in NetworkInterface.GetAllNetworkInterfaces()){
                if(n.OperationalStatus!=OperationalStatus.Up)continue;
                var nm=n.Name.ToLower();var ds=n.Description.ToLower();
                if(NicKeys.Any(k=>nm.Contains(k)||ds.Contains(k)))return new VpnStatus{IsActive=true,Name=n.Name};}}catch{}
            return new VpnStatus{IsActive=false,Name=""};
        }
    }
}
