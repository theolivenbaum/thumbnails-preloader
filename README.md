# ThumbsPreloader

A modernized .NET 10 / [Avalonia UI](https://avaloniaui.net/) rewrite of
[bruhov/WinThumbsPreloader](https://github.com/bruhov/WinThumbsPreloader). It walks a
directory tree and asks the Windows Shell thumbnail cache to extract a preview for every
file, so file managers display thumbnails instantly afterward.

The original Windows Forms code is preserved under [`.old/`](.old/) for reference.

## Highlights

- .NET 10, Avalonia 11, Fluent theme.
- Windows-only — the app targets `net10.0-windows` because thumbnail extraction goes
  through the Windows Shell `IThumbnailCache` COM interface.
- **Nested progress bars** — one bar per directory currently being processed, stacked by
  depth, so you can watch the recursion fan in and out as the cache warms.
- Same CLI surface as the original (`ThumbsPreloader [rs] <path>`).

## Build

```sh
dotnet build src/ThumbsPreloader/ThumbsPreloader.csproj -c Release
```

To produce a self-contained Windows executable:

```sh
dotnet publish src/ThumbsPreloader/ThumbsPreloader.csproj \
    -c Release -r win-x64 --self-contained true \
    -p:PublishSingleFile=true
```

## Usage

```
ThumbsPreloader [rs] <path>
```

| Flag | Meaning                                      |
|------|----------------------------------------------|
| `r`  | Recurse into subdirectories                  |
| `s`  | Silent mode — no UI, run and exit            |

With no flags, only the entries of `<path>` are processed.
With no arguments, an About window is shown.

Examples:

```sh
ThumbsPreloader "C:\Users\me\Pictures"
ThumbsPreloader r "D:\Photo Library"
ThumbsPreloader rs "E:\Archive"
```

## How the progress UI works

For every directory the worker walks into, a new `DirectoryProgress` node is pushed onto
a stack and rendered as its own progress bar. When the worker returns from that
directory, the node is popped. At any moment the window shows:

- the top-level root progress, plus
- one nested bar for each currently-active depth, with the deepest one being the
  directory whose entry is being processed right now.

Each bar tracks its own item count and current filename. The UI updates on the
Avalonia dispatcher; the COM work happens on a background thread.

## Platform notes

This project targets `net10.0-windows` and uses `IThumbnailCache` from `shell32.dll`,
so it builds and runs on Windows only.

## License

MIT — see [`LICENSE`](LICENSE).
