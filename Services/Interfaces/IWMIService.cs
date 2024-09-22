

using LauncherApp.Models;

namespace LauncherApp.Services.Interfaces;

public interface IWMIService
{
    Task<List<WMIProcess>> GetProcesses();
}