# WinFormsApp

Empty Windows Forms desktop application targeting .NET 8 (LTS).

## Requirements

- Windows 10 or later (WinForms builds and runs on Windows only)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (optional, needed for the WinForms visual designer)

## Layout

```
WinFormsApp.sln
src/
  WinFormsApp/
    WinFormsApp.csproj
    Program.cs             application entry point
    MainForm.cs            form code
    MainForm.Designer.cs   designer-generated layout
    app.manifest           per-monitor V2 DPI awareness, long paths
```

## Build and run

```bash
dotnet restore
dotnet build
dotnet run --project src/WinFormsApp
```

Or open `WinFormsApp.sln` in Visual Studio and press F5.

## Publish a self-contained executable

```bash
dotnet publish src/WinFormsApp -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The result lands in `src/WinFormsApp/bin/Release/net8.0-windows/win-x64/publish/`.
