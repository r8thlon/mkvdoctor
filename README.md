# mkvdoctor

Video processing tool: fix corrupted duration metadata, concatenate videos, and convert any format to MKV.

Built with **C# .NET 8** + **Avalonia UI**, all within a **Distrobox** container.

## Requirements

- **Podman** (or Docker with Distrobox)
- **ffmpeg** on the host
- Make

## Quick start

```bash
make setup    # build container image + create distrobox (first time only)
make run      # build and launch the GUI
```

## Features

| Feature | Description |
|---|---|
| **Fix duration** | Remux with `-fflags +genpts -correct_ts_overflow 1` to repair corrupted duration metadata |
| **Concatenate** | Join multiple videos in sequence using the `concat` demuxer (stream copy) |
| **Convert to MKV** | Remux any video format to Matroska with codec copy |

## Architecture

```
src/MkvDoctor/
├── Core/                         # No dependencies on UI framework
│   ├── Interfaces/               # IVideoOperation, IFFmpegService
│   ├── Models/                   # VideoFile, OperationResult
│   ├── Services/                 # FFmpegService, OperationRunner
│   └── Operations/               # FixDuration, Concatenate, ConvertToMkv + Factory
└── UI/                           # Avalonia-specific
    ├── App.axaml                 # Application + FluentTheme
    ├── RelayCommand.cs           # ICommand implementation
    ├── ViewModels/               # MainViewModel, VideoFileModel
    └── Views/                    # MainWindow (XAML + code-behind)
```

### SOLID, KISS, DRY

- **Single responsibility**: one class per operation
- **Open/Closed**: `IVideoOperation` + `OperationFactory` — new operations plug in without touching existing code
- **DRY**: all business logic in Core, shared by any future frontend
- **KISS**: every video operation is a thin wrapper around a single ffmpeg command

### Build environment

| Component | Where |
|---|---|
| .NET SDK | Container |
| Avalonia NuGet | Container (restored at build time) |
| ffmpeg | Container (runtime) |
| Source code | Host (mounted via Distrobox) |
| GUI display | Host (Wayland/X11 forwarded) |

## Commands

```bash
make setup      # build container + create distrobox
make build      # compile
make run        # compile + launch GUI
make clean      # clean artifacts
make shell      # enter distrobox shell
make rebuild    # clean container + rebuild from scratch
```
