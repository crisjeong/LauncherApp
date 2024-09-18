
using System.ComponentModel;

namespace LauncherApp.Models;

[Serializable]
public class ProgramInfo : INotifyPropertyChanged
{
    private string name;
    private string filePath;
    private string version;
    private string isRunning;

    public string Name
    {
        get => name;
        set
        {
            if (name != value)
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public string FilePath
    {
        get => filePath;
        set
        {
            if (filePath != value)
            {
                filePath = value;
                OnPropertyChanged(nameof(FilePath));
            }
        }
    }

    public string Version
    {
        get => version;
        set
        {
            if (version != value)
            {
                version = value;
                OnPropertyChanged(nameof(Version));
            }
        }
    }

    public string IsRunning
    {
        get => isRunning;
        set
        {
            if (isRunning != value)
            {
                isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
