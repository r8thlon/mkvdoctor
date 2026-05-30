using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using MkvDoctor.Core.Services;
using MkvDoctor.UI.ViewModels;

namespace MkvDoctor.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var ffmpeg = new FFmpegService();
        var runner = new OperationRunner(ffmpeg);
        _viewModel = new MainViewModel(runner);
        DataContext = _viewModel;

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
    }

    private void OnSelectAllClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.ToggleSelectAll();
    }

    private async void OnAddFilesClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Video Files")
                {
                    Patterns = ["*.mp4", "*.avi", "*.mov", "*.mkv", "*.webm", "*.flv", "*.wmv", "*.mts", "*.m2ts"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*.*"]
                }
            ]
        });

        if (files.Count > 0)
        {
            var paths = files
                .Select(f => f.TryGetLocalPath())
                .Where(p => p is not null)
                .Select(p => p!)
                .ToArray();
            _viewModel.AddFiles(paths);
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618
        if (e.Data.Contains(DataFormats.FileNames))
        {
            var files = e.Data.GetFileNames()?.ToArray();
            if (files is { Length: > 0 })
                _viewModel.AddFiles(files);
        }
#pragma warning restore CS0618
        dropBorder.Background = new SolidColorBrush(Color.Parse("#f5f5f5"));
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618
        if (e.Data.Contains(DataFormats.FileNames))
        {
            e.DragEffects = DragDropEffects.Copy;
            dropBorder.Background = new SolidColorBrush(Color.Parse("#e0f0ff"));
        }
#pragma warning restore CS0618
    }

    private void OnDragLeave(object? sender, RoutedEventArgs e)
    {
        dropBorder.Background = new SolidColorBrush(Color.Parse("#f5f5f5"));
    }
}
