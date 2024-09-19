using LauncherApp.Data;
using LauncherApp.Models;
using Microsoft.Win32;
using System.Windows;

namespace LauncherApp.Views;

/// <summary>
/// ProgramDialog.xaml에 대한 상호 작용 논리
/// </summary>
public partial class ProgramDialog : Window
{
    public ProgramInfo Program { get; private set; }
    public ActionMode Mode { get; set; }

    public ProgramDialog(ActionMode mode, ProgramInfo program = null, bool isReadOnly = false)
    {
        InitializeComponent();
        Mode = mode;
        SetButtonContent();

        if (program != null)
        {
            ProgramNameTextBox.Text = program.Name;
            ExecutablePathTextBox.Text = program.FilePath;
            VersionTextBox.Text = program.Version;
            Program = program;
        }
        else
        {
            Program = new ProgramInfo();
        }

        if (isReadOnly)
        {
            ProgramNameTextBox.IsEnabled = false;
            ExecutablePathTextBox.IsEnabled = false;
            VersionTextBox.IsEnabled = false;
            //ApplyButton.IsEnabled = false;            
        }
    }

    private void SetButtonContent()
    {
        switch (Mode)
        {
            case ActionMode.Create:
                ApplyButton.Content = "Add";
                break;
            case ActionMode.Update:
                ApplyButton.Content = "Update";
                break;
            case ActionMode.Delete:
                ApplyButton.Content = "Delete";
                break;

            default: break;
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        // Ensure Program is not null
        if (Program == null)
        {
            Program = new ProgramInfo();
        }

        Program.Name = ProgramNameTextBox.Text;
        Program.FilePath = ExecutablePathTextBox.Text;
        Program.Version = VersionTextBox.Text;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        //openFileDialog.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
        openFileDialog.Filter = "Executable Files (*.exe)|*.exe|Java Archives (*.jar)|*.jar|All Files (*.*)|*.*";
        openFileDialog.Title = "Select Executable File";

        if (openFileDialog.ShowDialog() == true)
        {
            ProgramNameTextBox.Text = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);
            ExecutablePathTextBox.Text = openFileDialog.FileName;
        }
    }
}
