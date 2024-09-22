using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Windows;
using LauncherApp.Models;
using LauncherApp.Services.Interfaces;

namespace LauncherApp.Services;

public class ProgramManager : IProgramManager
{
    private ObservableCollection<ProgramInfo> Programs;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public void Add(ProgramInfo programInfo)
    {
        Programs?.Add(programInfo);
    }

    public void Remove(ProgramInfo programInfo)
    {
        Programs?.Remove(programInfo);
    }

    public bool? Find(ProgramInfo programInfo)
    {
        return Programs?.Any(p => p.Name.Equals(programInfo.Name, StringComparison.OrdinalIgnoreCase) &&
                                  p.FilePath.Equals(programInfo.FilePath, StringComparison.OrdinalIgnoreCase));
    }

    public ObservableCollection<ProgramInfo> Get()
    {
        return Programs;
    }

    public async Task<ObservableCollection<ProgramInfo>> LoadFromFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                await using var fileStream = File.OpenRead(filePath);
                Programs = await JsonSerializer.DeserializeAsync<ObservableCollection<ProgramInfo>>(fileStream) ?? new ObservableCollection<ProgramInfo>();
                await fileStream.DisposeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load program list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Programs = new ObservableCollection<ProgramInfo>();
            }
        }
        else
        {
            Programs = new ObservableCollection<ProgramInfo>();
        }

        return Programs;
    }

    public async Task SaveToFile(string filePath)
    {
        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, Programs, new JsonSerializerOptions { WriteIndented = true });
        await fileStream.DisposeAsync();
    }
}
