using System.Collections.ObjectModel;
using System.Collections.Specialized;
using LauncherApp.Models;

namespace LauncherApp.Services.Interfaces;

public interface IProgramManager
{
    event NotifyCollectionChangedEventHandler? CollectionChanged;
    void Remove(ProgramInfo programInfo);
    Task<ObservableCollection<ProgramInfo>> LoadFromFile(string filePath);
    Task SaveToFile(string filePath);
    void Add(ProgramInfo programInfo);
    bool? Find(ProgramInfo programInfo);
    ObservableCollection<ProgramInfo> Get();
}