using LauncherApp.Models;
using LauncherApp.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LauncherApp.Services;

public class WMIWorker : BackgroundService
{
    private readonly ILogger<WMIWorker> _logger;
    private readonly IWMIService _wmiService;
    private readonly IProgramManager _programManager;

    public WMIWorker(ILogger<WMIWorker> logger, IWMIService wmiService, IProgramManager programManager)
    {
        _logger = logger;
        _wmiService = wmiService;
        _programManager = programManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_programManager.Get() != null)
                {
                    foreach (var program in _programManager.Get())
                    {
                        program.IsRunning = (await IsRunning(program)) == true ? "Running" : "Not Running";
                    }
                }



                //List<Process> processes1 = (await _wmiService.GetProcesses()).Where(p => p.Pid != 0).ToList();
                //await Task.Delay(2000);
                //List<Process> processes2 = (await _wmiService.GetProcesses()).Where(p => p.Pid != 0).ToList();


                //List<Process> highCPUConsumingProccesses = new List<Process>();

                //foreach (Process p1 in processes1)
                //{
                //    Process p2 = processes2.Where(pr => pr.Pid == p1.Pid).SingleOrDefault();

                //    int procDiff = (int)(p2.PercentProcessorTime - p1.PercentProcessorTime);
                //    int timeDiff = (int)(p2.TimeStamp_Sys100NS - p1.TimeStamp_Sys100NS);

                //    double percentUsage = Math.Round(((double)procDiff / (double)timeDiff) * 100, 2);

                //    Console.WriteLine($"{p1.Name}: {percentUsage}%");

                //    if (percentUsage > 80)
                //    {
                //        p1.CpuUsagePercent = percentUsage;
                //        highCPUConsumingProccesses.Add(p1);
                //    }
                //}

                //if (highCPUConsumingProccesses.Any())
                //{
                //    SendMail(System.Environment.GetEnvironmentVariable("COMPUTERNAME"), highCPUConsumingProccesses);
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            await Task.Delay(1 * 1000, stoppingToken);
        }
    }

    private async Task<bool> IsRunning(ProgramInfo programInfo)
    {
        string processName = System.IO.Path.GetFileNameWithoutExtension(programInfo.FilePath);
        List<WMIProcess> processes = (await _wmiService.GetProcesses()).Where(p => p.ProcessID != 0 && p.Name == processName).ToList();

        //List<WMIProcess> processes = await _wmiService.GetProcesses();

        //foreach (WMIProcess process in processes)
        //{
        //    if (process.ProcessID == 0)
        //        continue;

        //    if (process.Name == $"{processName}.exe")
        //    {
        //        return true;
        //    }
        //}

        return processes.Any();


        //if (programInfo.FilePath == null)
        //{
        //    return false;
        //}

        //string extension = System.IO.Path.GetExtension(programInfo.FilePath).ToLower();
        //string jarStater = "java";

        //if (extension == ".jar")
        //{
        //    var javaProcesses = await _wmiService.GetProcessesByName(jarStater); //Process.GetProcessesByName(jarStater);
        //    foreach (var process in javaProcesses)
        //    {
        //        try
        //        {
        //            // 각 프로세스의 명령줄을 통해 JAR 파일이 실행 중인지 확인
        //            //if (process.MainModule != null && process.MainModule.FileName.EndsWith($"{jarStater}.exe"))
        //            //{
        //            //    var commandLine = await _wmiService.GetCommandLine(process); //GetCommandLine(process);
        //            //    if (commandLine != null && commandLine.Contains(programInfo.FilePath))
        //            //    {
        //            //        return true;
        //            //    }
        //            //}
        //        }
        //        catch (Exception)
        //        {
        //            // 프로세스에 접근할 수 없을 때 발생하는 예외 무시
        //        }
        //    }
        //    return false;
        //}
        //else
        //{
        //    // 일반 실행 파일의 경우, 프로세스 이름으로 확인
        //    string processName = System.IO.Path.GetFileNameWithoutExtension(programInfo.FilePath);
        //    return (await _wmiService.GetProcessesByName(processName)).Any();
        //}
    }

}
