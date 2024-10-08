﻿using LauncherApp.Command;
using LauncherApp.Data;
using LauncherApp.Models;
using LauncherApp.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace LauncherApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum ResizeDirection
        {
            Left = 1,
            Right = 2,
            Top = 3,
            TopLeft = 4,
            TopRight = 5,
            Bottom = 6,
            BottomLeft = 7,
            BottomRight = 8
        }

        public ICommand RunActionCommand { get; }
        public ICommand DeleteActionCommand { get; }
        //private List<ProgramInfo> programList = new List<ProgramInfo>();
        private ObservableCollection<ProgramInfo> programList;
        private DispatcherTimer timer;

        //private const string DataFilePath = "programs.json"; // 데이터 파일 경로

        private readonly ILogger<MainWindow> _logger;
        private readonly AppSettings _appSettings;
        private readonly IOptionsMonitor<AppSettings> _appSettingsMonitor;


        public MainWindow(ILogger<MainWindow> logger, IOptionsMonitor<AppSettings> appSettingsMonitor/*AppSettings appSettings*/)
        {
            InitializeComponent();

            DataContext = this;

            _logger = logger;

            _appSettings = appSettingsMonitor.CurrentValue;
            _appSettingsMonitor = appSettingsMonitor;

            //변경될 때마다 반영
            _appSettingsMonitor.OnChange(settings =>
            {
                Application.Current.Dispatcher.Invoke(() => ApplySettings(settings));
            });


            RunActionCommand = new RelayCommand<ProgramInfo>(ExecuteAction);
            DeleteActionCommand = new RelayCommand<ProgramInfo>(DeleteAction);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            //ProgramsDataGrid.ItemsSource = programList;
            //programList = new ObservableCollection<ProgramInfo>();

            LoadProgramList(_appSettings.DataFilePath);

            programList.CollectionChanged += ProgramList_CollectionChanged; // CollectionChanged 이벤트 처리
            ProgramsDataGrid.Loaded += ProgramsDataGrid_Loaded;
            ProgramsDataGrid.LayoutUpdated += DataGrid_LayoutUpdated;

            // Initialize Edit and Delete buttons as disabled
            EditButton.IsEnabled = false;
            //DeleteButton.IsEnabled = false;

            Title = _appSettings.ApplicationName;

            _logger.LogInformation("MainWindow initialized");
        }

        private void ApplySettings(AppSettings settings)
        {
            _appSettings.ApplicationName = settings.ApplicationName;
            _appSettings.Version = settings.Version;
            _appSettings.DataFilePath = settings.DataFilePath;
            _appSettings.JarStater = settings.JarStater;
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 창 크기에 따라 DataGrid의 크기를 조정
            double newWidth = e.NewSize.Width - 40;  // 좌우 마진을 고려
            double newHeight = e.NewSize.Height - 106; // 상단 마진(30px)과 상하단 여백을 고려

            if (newWidth > 0)
            {
                ProgramsDataGrid.Width = newWidth;
            }

            if (newHeight > 0)
            {
                ProgramsDataGrid.Height = newHeight;
            }

            //if (ProgramsDataGrid.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                AdjustColumnWidths();
            }

        }

        private void CaptionBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 창을 드래그할 수 있도록 합니다.
            this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadProgramList(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string jsonData = File.ReadAllText(filePath);
                    programList = JsonSerializer.Deserialize<ObservableCollection<ProgramInfo>>(jsonData) ?? new ObservableCollection<ProgramInfo>();

                    _logger.LogInformation("Program list loaded");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load program list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    programList = new ObservableCollection<ProgramInfo>();
                }
            }
            else
            {
                programList = new ObservableCollection<ProgramInfo>();
            }

            ProgramsDataGrid.ItemsSource = programList;
            ProgramsDataGrid.SelectedIndex = -1; // SelectedIndex 초기화
        }

        private void SaveProgramList(string filePath)
        {
            try
            {
                string jsonData = JsonSerializer.Serialize(programList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save program list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGrid_LayoutUpdated(object sender, EventArgs e)
        {
            // DataGrid의 모든 시각적 요소가 렌더링된 후에 호출됩니다.
            AdjustColumnWidths();

            // 필요한 경우 이벤트 핸들러를 해제하여 반복 호출 방지
            ProgramsDataGrid.LayoutUpdated -= DataGrid_LayoutUpdated;
        }

        private void ProgramsDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            AdjustColumnWidths();
        }

        private void ProgramList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

            {
                AdjustColumnWidths();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 데이터 로딩 이후에 Grid의 아이템 카운트를 확인
            //var itemCount = ProgramsDataGrid.Items.Count;
            //MessageBox.Show($"Items count: {itemCount}");
        }

        private void AdjustColumnWidths()
        {
            if (!ProgramsDataGrid.IsLoaded || (ProgramsDataGrid.Items.Count <= 0))
            {
                return;
            }


            // 전체 DataGrid 너비에서 가용 너비 계산
            double availableWidth = ProgramsDataGrid.ActualWidth - 20; // 여기서 20은 가중치를 고려한 Margin
            double totalTextWidth = 0;
            int columnCount = ProgramsDataGrid.Columns.Count;

            // 각 열의 최대 텍스트 너비를 계산하여 전체 텍스트 너비를 구함
            foreach (var column in ProgramsDataGrid.Columns)
            {
                double maxTextWidth = CalculateMaxTextWidth(column);
                totalTextWidth += maxTextWidth;
            }

            // 비율에 따른 각 열의 너비를 재조정
            foreach (var column in ProgramsDataGrid.Columns)
            {
                double maxTextWidth = CalculateMaxTextWidth(column);

                // 전체 텍스트 너비에 비례하여 열 너비 계산
                double widthRatio = maxTextWidth / totalTextWidth;
                double adjustedWidth = availableWidth * widthRatio;

                // 열 너비가 과도하게 커지지 않도록 설정
                column.Width = new DataGridLength(adjustedWidth, DataGridLengthUnitType.Pixel);
            }

            // 마지막 열에 나머지 너비를 할당하지 않도록 설정
            var lastColumn = ProgramsDataGrid.Columns.Last();
            lastColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        }

        private double CalculateMaxTextWidth(DataGridColumn column)
        {
            double maxTextWidth = 1;

            // 각 열의 셀 데이터를 반복하여 최대 텍스트 길이 계산
            foreach (var item in ProgramsDataGrid.Items)
            {
                var cellContent = column.GetCellContent(item) as TextBlock;
                if (cellContent != null)
                {
                    // 텍스트 블록의 실제 길이를 계산
                    var formattedText = new FormattedText(
                        cellContent.Text,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(cellContent.FontFamily, cellContent.FontStyle, cellContent.FontWeight, cellContent.FontStretch),
                        cellContent.FontSize,
                        Brushes.Black,
                        new NumberSubstitution(),
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    if (formattedText.Width > maxTextWidth)
                    {
                        maxTextWidth = formattedText.Width;
                    }
                }
            }

            return maxTextWidth;
        }

        private void ExecuteAction(ProgramInfo program)
        {
            if (program.IsRunning == "Running")
            {
                StopProgram(program);
            }
            else
            {
                RunProgram(program);
            }
        }

        private void DeleteAction(ProgramInfo program)
        {
            var selectedProgram = ProgramsDataGrid.SelectedItem as ProgramInfo;
            if (selectedProgram != null)
            {
                // 프로그램의 상태가 Running인 경우 삭제를 차단하고 오류 메시지를 표시
                if (selectedProgram.IsRunning.Equals("Running", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("The selected program is currently running and cannot be deleted.",
                                    "Delete Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
                else
                {
                    // 프로그램이 실행 중이 아닌 경우에만 삭제 허용
                    var dialog = new ProgramDialog(ActionMode.Delete, selectedProgram, isReadOnly: true);
                    dialog.Owner = this;
                    if (dialog.ShowDialog() == true)
                    {
                        programList.Remove(selectedProgram);

                        ProgramsDataGrid.Items.Refresh();
                        SaveProgramList(_appSettings.DataFilePath); // 수정 후 목록을 저장
                    }
                }
            }
        }

        private Process? FindRunningJavaProgramByName(String name)
        {
            var javaProcesses = Process.GetProcessesByName(_appSettings.JarStater);
            foreach (var process in javaProcesses)
            {
                try
                {
                    // WMI를 사용해 프로세스의 명령줄을 가져와 해당 JAR 파일이 포함된 프로세스를 찾음
                    string commandLine = GetCommandLine(process);
                    if (commandLine != null && commandLine.Contains(name))
                    {
                        return process;
                    }
                }
                catch (Exception ex)
                {
                    // 프로세스 접근 불가 시 예외 무시
                    MessageBox.Show($"프로세스 접근 불가합니다.: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }
            }

            return null;
        }


        private void RunProgram(ProgramInfo program)
        {
            try
            {
                //Process.Start(program.FilePath);

                // 파일 경로의 확장자 확인
                string extension = System.IO.Path.GetExtension(program.FilePath).ToLower();

                ProcessStartInfo startInfo;

                if (extension == ".jar")
                {
                    // JAR 파일 실행을 위한 Java 명령어 구성
                    //startInfo = new ProcessStartInfo("java", $"-jar \"{program.FilePath}\"");

                    int retryCount = 5;
                    while (retryCount > 0)
                    {
                        Process? process = FindRunningJavaProgramByName(program.Name);
                        if (process != null)
                        {
                            MessageBox.Show($"동일한 이름의 프로세스가 이미 실행 중입니다. \n동일한 이름의 프로그램을 종료하고 선택하신 프로그램을 실행합니다.", "Infomation", MessageBoxButton.OK);
                            process.Kill();

                            //강제 delay
                            Task.Delay(1 * 1000);

                            // 동일한 이름의 프로그램 종료 완료
                            break;
                        }

                        retryCount--;
                    }

                    // JAR 파일 실행을 위한 Java 명령어 구성
                    //startInfo = new ProcessStartInfo("java", $"-jar \"{program.FilePath}\"");
                    //startInfo = new ProcessStartInfo(_appSettings.JarStater, $"-jar \"{program.FilePath}\"");

                    //StartJarFile(program.FilePath);

                    Process processJar = new Process();
                    processJar.EnableRaisingEvents = false;
                    processJar.StartInfo.FileName = "java.exe";
                    processJar.StartInfo.Arguments = "-jar " + '"' + program.FilePath;
                    //processJar.StartInfo.Verb = "runas";
                    processJar.Start();

                }
                else
                {
                    // 일반 실행 파일 실행
                    startInfo = new ProcessStartInfo(program.FilePath);
                    startInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(program.FilePath);
                    startInfo.UseShellExecute = true;

                    Process.Start(startInfo);
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting program: {ex.Message}");
            }
        }

        public void StartJarFile(string jarFilePath)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar \"{jarFilePath}\"",
                    //RedirectStandardOutput = true,
                    //RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    // Optional: Read the output and error streams if needed
                    //string output = process.StandardOutput.ReadToEnd();
                    //string error = process.StandardError.ReadToEnd();

                    //process.WaitForExit();

                    //if (process.ExitCode != 0)
                    //{
                    //    // Handle error
                    //    throw new Exception($"Error executing jar file: {error}");
                    //}
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                MessageBox.Show($"Failed to start jar file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopProgram(ProgramInfo program)
        {
            try
            {
                string extension = System.IO.Path.GetExtension(program.FilePath).ToLower();

                if (extension == ".jar")
                {
                    // Java 프로세스를 찾아서 해당 JAR 파일을 실행 중인 프로세스를 종료
                    var javaProcesses = Process.GetProcessesByName(_appSettings.JarStater);
                    foreach (var process in javaProcesses)
                    {
                        try
                        {
                            // WMI를 사용해 프로세스의 명령줄을 가져와 해당 JAR 파일이 포함된 프로세스를 찾음
                            string commandLine = GetCommandLine(process);
                            if (commandLine != null && commandLine.Contains(program.FilePath))
                            {
                                process.Kill();
                                break;
                            }
                        }
                        catch
                        {
                            // 프로세스 접근 불가 시 예외 무시
                        }
                    }
                }
                else
                {
                    // 일반 실행 파일의 경우, 프로세스 이름으로 종료
                    string processName = System.IO.Path.GetFileNameWithoutExtension(program.FilePath);
                    var processes = Process.GetProcessesByName(processName);
                    foreach (var process in processes)
                    {
                        process.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to stop program: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            //var selectedProgram = ProgramsDataGrid.SelectedItem as ProgramInfo;
            //int selectedIndex = ProgramsDataGrid.SelectedIndex;
            //string selectedFilePath = selectedProgram?.FilePath;
            

            foreach (var program in programList)
            {
                //string processName = Path.GetFileNameWithoutExtension(program.FilePath);
                //var processes = Process.GetProcessesByName(processName);
                //program.IsRunning = processes.Length > 0 ? "Running" : "Not Running";
                if (IsProgramRunning(program))
                {
                    program.IsRunning = "Running";
                }
                else
                {
                    program.IsRunning = "Not Running";
                }
            }

            /*
                        ProgramsDataGrid.Items.Refresh();

                        // Re-select the previously selected item
                        if (selectedIndex >= 0 && selectedIndex < ProgramsDataGrid.Items.Count)
                        {
                            ProgramsDataGrid.SelectedIndex = selectedIndex;
                        }
                        else if (selectedProgram != null)
                        {
                            // Fallback to selecting by reference if index is invalid
                            ProgramsDataGrid.SelectedItem = programList.FirstOrDefault(p => p.Name == selectedProgram.Name && p.FilePath == selectedProgram.FilePath);
                        }

                        // Ensure the selected row style is properly applied
                        if (ProgramsDataGrid.SelectedItem != null)
                        {
                            ProgramsDataGrid.ScrollIntoView(ProgramsDataGrid.SelectedItem);
                            DataGridRow row = (DataGridRow)ProgramsDataGrid.ItemContainerGenerator.ContainerFromItem(ProgramsDataGrid.SelectedItem);
                            if (row != null)
                            {
                                row.IsSelected = true;
                                row.MoveFocus(new TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
                            }                
                        }
            */
        }

        private bool IsProgramRunning(ProgramInfo program)
        {
            if (program.FilePath == null)
            {
                return false;
            }

            string extension = System.IO.Path.GetExtension(program.FilePath).ToLower();

            if (extension == ".jar")
            {
                var javaProcesses = Process.GetProcessesByName(_appSettings.JarStater);
                foreach (var process in javaProcesses)
                {
                    try
                    {
                        // 각 프로세스의 명령줄을 통해 JAR 파일이 실행 중인지 확인
                        if (process.MainModule != null && process.MainModule.FileName.EndsWith($"{_appSettings.JarStater}.exe"))
                        {
                            var commandLine = GetCommandLine(process);
                            if (commandLine != null && commandLine.Contains(program.FilePath))
                            {
                                return true;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // 프로세스에 접근할 수 없을 때 발생하는 예외 무시
                    }
                }
                return false;
            }
            else
            {
                // 일반 실행 파일의 경우, 프로세스 이름으로 확인
                string processName = System.IO.Path.GetFileNameWithoutExtension(program.FilePath);
                var processes = Process.GetProcessesByName(processName);
                return processes.Length > 0;
            }
        }

        private string? GetCommandLine(Process process)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
                using (var objects = searcher.Get())
                {
                    return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProgramDialog(ActionMode.Create);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                // 입력된 프로그램 이름이 이미 목록에 있는지 확인
                bool isDuplicate = programList.Any(p => p.Name.Equals(dialog.Program.Name, StringComparison.OrdinalIgnoreCase) &&
                                                        p.FilePath.Equals(dialog.Program.FilePath, StringComparison.OrdinalIgnoreCase));

                if (isDuplicate)
                {
                    // 중복된 경우 에러 메시지 박스를 표시
                    MessageBox.Show("A program with the same name already exists. Please choose a different name.",
                                    "Duplicate Program Name",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
                else
                {
                    if (!String.IsNullOrEmpty(dialog.Program.Name))
                    {
                        //programList.Add(dialog.Program);
                        programList.Add(new ProgramInfo
                        {
                            Name = dialog.Program.Name,
                            FilePath = dialog.Program.FilePath,
                            Version = dialog.Program.Version,
                            IsRunning = "Not Running"
                        });

                        ProgramsDataGrid.Items.Refresh();
                        SaveProgramList(_appSettings.DataFilePath); // 수정 후 목록을 저장
                    }
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProgramsDataGrid.SelectedItem is ProgramInfo selectedProgram)
            {
                var dialog = new ProgramDialog(ActionMode.Update, selectedProgram, isReadOnly: true);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    selectedProgram.Name = dialog.Program.Name;
                    selectedProgram.FilePath = dialog.Program.FilePath;
                    selectedProgram.Version = dialog.Program.Version;

                    ProgramsDataGrid.Items.Refresh();
                    SaveProgramList(_appSettings.DataFilePath); // 수정 후 목록을 저장
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedProgram = ProgramsDataGrid.SelectedItem as ProgramInfo;
            if (selectedProgram != null)
            {
                // 프로그램의 상태가 Running인 경우 삭제를 차단하고 오류 메시지를 표시
                if (selectedProgram.IsRunning.Equals("Running", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("The selected program is currently running and cannot be deleted.",
                                    "Delete Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
                else
                {
                    // 프로그램이 실행 중이 아닌 경우에만 삭제 허용
                    var dialog = new ProgramDialog(ActionMode.Delete, selectedProgram, isReadOnly: true);
                    dialog.Owner = this;
                    if (dialog.ShowDialog() == true)
                    {
                        programList.Remove(selectedProgram);

                        ProgramsDataGrid.Items.Refresh();
                        SaveProgramList(_appSettings.DataFilePath); // 수정 후 목록을 저장
                    }
                }
            }


            //if (ProgramsDataGrid.SelectedItem is ProgramInfo selectedProgram)
            //{
            //    var dialog = new ProgramDialog(selectedProgram, isReadOnly: true);
            //    dialog.Owner = this;
            //    if (dialog.ShowDialog() == true)
            //    {
            //        programList.Remove(selectedProgram);

            //        ProgramsDataGrid.Items.Refresh();
            //        SaveProgramList(); // 수정 후 목록을 저장
            //    }
            //}
        }

        private void ProgramsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Handle selection changed event if needed
            var selectedProgram = ProgramsDataGrid.SelectedItem as ProgramInfo;

            EditButton.IsEnabled = ProgramsDataGrid.SelectedItem != null;
            //DeleteButton.IsEnabled = ProgramsDataGrid.SelectedItem != null;
        }

        private void ResizeWindow(ResizeDirection direction)
        {
            HwndSource hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            SendMessage(hwndSource.Handle, 0x112, (IntPtr)(61440 + direction), IntPtr.Zero);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private void Resize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle clickedRectangle = sender as Rectangle;

            if (clickedRectangle != null)
            {
                switch (clickedRectangle.Name)
                {
                    case "top":
                        ResizeWindow(ResizeDirection.Top);
                        break;
                    case "bottom":
                        ResizeWindow(ResizeDirection.Bottom);
                        break;
                    case "left":
                        ResizeWindow(ResizeDirection.Left);
                        break;
                    case "right":
                        ResizeWindow(ResizeDirection.Right);
                        break;
                    case "topLeft":
                        ResizeWindow(ResizeDirection.TopLeft);
                        break;
                    case "topRight":
                        ResizeWindow(ResizeDirection.TopRight);
                        break;
                    case "bottomLeft":
                        ResizeWindow(ResizeDirection.BottomLeft);
                        break;
                    case "bottomRight":
                        ResizeWindow(ResizeDirection.BottomRight);
                        break;
                }
            }
        }
    }
}