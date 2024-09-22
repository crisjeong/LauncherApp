using ORMi;

namespace LauncherApp.Models;

[WMIClass(Name = "Win32_Process")]
public class WMIProcess
{    
    public string? Name { get; set; }
    public int ProcessID { get; set; }    

    public string? CommandLine { get; set; }
}
