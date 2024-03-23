using System.Diagnostics.CodeAnalysis;
using System.Net;
using JetBrains.Annotations;
using MicrosoftIpNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace CF.AccessProxy.Models;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public record IpNetwork
{
    private readonly string _string;
    private readonly MicrosoftIpNetwork _network;

    public IpNetwork(string s) : this(s, new MicrosoftIpNetwork(IPAddress.None, 0)) { }
    
    public IpNetwork(string s, MicrosoftIpNetwork network)
    {
        _string = s;
        _network = network;
    }

    public static bool TryParse(string ipNetwork, [NotNullWhen(true)] out IpNetwork? result)
    {
        if (!MicrosoftIpNetwork.TryParse(ipNetwork, out var microsoftIpNetwork))
        {
            result = default;
            return false;
        }

        result = new IpNetwork(ipNetwork, microsoftIpNetwork);
        return true;
    }
    
    public bool Contains(IPAddress address)
    {
        return _network.Contains(address);
    }
    
    public override string ToString()
    {
        return _string;
    }
}