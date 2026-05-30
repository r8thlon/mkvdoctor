using System.Diagnostics;
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
    private int _mixDragSourceIndex = -1;

    public MainWindow()
    {
        InitializeComponent();

        var ffmpeg = new FFmpegService();
        var runner = new OperationRunner(ffmpeg);
        _viewModel = new MainViewModel(runner);
        DataContext = _viewModel;

        DragDrop.SetAllowDrop(this, true);
        DragDrop.SetAllowDrop(mixListBox, true);
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnWindowDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnWindowDragLeave);
    }

    // ── Process tab ──────────────────────────────────────────────

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

    private void OnProcessOpenLocationClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: VideoFileModel { OutputPath: { } path } })
        {
            var dir = Path.GetDirectoryName(path);
            if (dir is not null)
                Process.Start("xdg-open", dir);
        }
    }

    // ── Mix tab ──────────────────────────────────────────────────

    private async void OnMixAddFilesClick(object? sender, RoutedEventArgs e)
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
            _viewModel.AddMixFiles(paths);
        }
    }

    private void OnMixItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.DataContext is VideoFileModel model)
        {
            _mixDragSourceIndex = _viewModel.MixFiles.IndexOf(model);
#pragma warning disable CS0618
            var data = new DataObject();
            data.Set("VideoFileModel", model);
            DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
#pragma warning restore CS0618
        }
    }

    private void OnMixListDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Move;
    }

    private void OnMixListDrop(object? sender, DragEventArgs e)
    {
        if (_mixDragSourceIndex < 0) return;

        var pos = e.GetPosition(mixListBox);

        var container = mixListBox.ContainerFromIndex(0) as Control;
        if (container == null) return;

        var itemHeight = container.Bounds.Height;
        if (itemHeight <= 0) return;

        var targetIndex = (int)(pos.Y / itemHeight);
        targetIndex = Math.Clamp(targetIndex, 0, _viewModel.MixFiles.Count);

        if (targetIndex != _mixDragSourceIndex)
        {
            if (targetIndex > _mixDragSourceIndex)
                targetIndex--;
            _viewModel.MixMoveItem(_mixDragSourceIndex, targetIndex);
        }

        _mixDragSourceIndex = -1;
    }

    private void OnMixOpenFolderClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.MixOutputPath is { } path)
        {
            var dir = Path.GetDirectoryName(path);
            if (dir is not null)
                Process.Start("xdg-open", dir);
        }
    }

    // ── Window drag & drop ───────────────────────────────────────

    private void OnDrop(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618
        if (e.Data.Contains(DataFormats.FileNames))
        {
            var files = e.Data.GetFileNames()?.ToArray();
            if (files is { Length: > 0 })
            {
                if (_viewModel.SelectedTabIndex == 0)
                    _viewModel.AddFiles(files);
                else
                    _viewModel.AddMixFiles(files);
            }
        }
#pragma warning restore CS0618

        processDropBorder.Background = new SolidColorBrush(Color.Parse("#f5f5f5"));
        mixDropBorder.Background = new SolidColorBrush(Color.Parse("#f5f5f5"));
    }

    private void OnWindowDragOver(object? sender, DragEventArgs e)
    {
        var border = _viewModel.SelectedTabIndex == 0 ? processDropBorder : mixDropBorder;

        processDropBorder.Background = new SolidColorBrush(Color.Parse("#f5f5f5"));
        mixDropBorder.Background = new SolidColorBrush(Color.Parse("#f5f5f5"));

#pragma warning disable CS0618
        if (e.Data.Contains(DataFormats.FileNames))
        {
            e.DragEffects = DragDropEffects.Copy;
            border.Background = new SolidColorBrush(Color.Parse("#e0f0ff"));
        }
#pragma warning restore CS0618
    }

    private void OnWindowDragLeave(object? sender, RoutedEventArgs e)
    {
        processDropBorder.Background = new SolidColorBrush(Color.Parse("#f5f5f5"));
        mixDropBorder.Background = new SolidColorBrush(Color.Parse("#f5f5f5"));
    }
}
