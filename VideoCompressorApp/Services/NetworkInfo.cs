using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace VideoCompressor.Services;

public static class NetworkInfo
{
    public static IReadOnlyList<string> GetLocalIPv4Addresses()
    {
        var result = new List<string>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        result.Add(addr.Address.ToString());
                }
            }
        }
        catch
        {
            // ambiente di rete non enumerabile: si mostra solo la porta
        }
        return result;
    }
}
