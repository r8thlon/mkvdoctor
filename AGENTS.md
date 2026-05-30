# mkvdoctor — AI Context

## Project overview

mkvdoctor is a cross-platform desktop GUI for fixing corrupted video duration metadata, concatenating videos, and converting to MKV. All video processing delegates to ffmpeg.

## Tech stack

- **Language**: C# (.NET 8)
- **UI framework**: Avalonia 11.3.13
- **Video engine**: ffmpeg (installed in container)
- **Build/runtime**: Docker + Distrobox (Ubuntu 24.04 container)
- **Build system**: Make

## Project structure

```
mkvdoctor/
├── distrobox/Containerfile       # Container definition
├── Makefile                      # All commands
├── src/MkvDoctor/
│   ├── Program.cs                # Entry point
│   ├── Core/                     # Business logic (no UI deps)
│   │   ├── Interfaces/           # IFFmpegService, IVideoOperation
│   │   ├── Models/               # VideoFile, OperationResult
│   │   ├── Services/             # FFmpegService, OperationRunner
│   │   └── Operations/           # FixDuration, Concatenate, ConvertToMkv, Factory
│   └── UI/                       # Avalonia layer
│       ├── App.axaml(.cs)        # Application definition
│       ├── RelayCommand.cs       # ICommand impl
│       ├── ViewModels/           # MainViewModel, VideoFileModel
│       └── Views/                # MainWindow.axaml(.cs)
```

## Key conventions

- **All ffmpeg calls** go through `FFmpegService` (wraps `System.Diagnostics.Process`)
- **Operations** implement `IVideoOperation` interface
- **New operation**: create class in `Operations/`, add enum to `OperationType`, add case to `OperationFactory.Create()`
- **ViewModels** use `INotifyPropertyChanged` + `RelayCommand` for data binding
- **No video parsing** in C# — everything delegated to ffmpeg CLI

## Build commands

```bash
make setup      # first time — builds docker container + creates distrobox
make build      # compile only
make run        # compile + launch GUI
make clean      # clean build artifacts
make rebuild    # distrobox rm + docker rmi && make setup
```

## FFmpeg operations

| Feature | ffmpeg args |
|---|---|
| Fix duration | `-i INPUT -c copy -map 0 -fflags +genpts -correct_ts_overflow 1 OUTPUT.mkv` |
| Concatenate | `-f concat -safe 0 -i FILELIST -c copy OUTPUT.mkv` |
| Convert to MKV | `-i INPUT -c copy -map 0 OUTPUT.mkv` |

## Design principles

- **SOLID**: interfaces for ffmpeg and operations, factory pattern, DI
- **CanCombineInputs**: operations declare whether they merge multiple inputs (Concatenate) or process per-file (FixDuration, ConvertToMkv)
- **DRY**: Core library is the single source of truth for all video logic
- **KISS**: no frame-level processing, no custom container parsing

## Container details

- Runtime: Docker (via distrobox)
- Image: Ubuntu 24.04
- User: same UID/GID as host (distrobox default)
- Home: host home mounted at same path
- X11: /tmp/.X11-unix forwarded via distrobox
- Wayland: /run/user/UID/wayland-0 forwarded
- ffmpeg: installed in container (6.1.1)
