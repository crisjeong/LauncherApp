using LauncherApp.Models;
using LauncherApp.Services.Interfaces;
using ORMi;

namespace LauncherApp.Services;

public class WMIService : IWMIService
{
    private WMIHelper _helper = new WMIHelper("root/CimV2");

    public async Task<List<WMIProcess>> GetProcesses()
    {
        List<WMIProcess> res = (await _helper.QueryAsync<WMIProcess>()).ToList();

        return res;
    }
}