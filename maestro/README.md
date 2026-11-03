# Maestro

## Linux setup

Needs .NET 9+ SDK, CMake, g++ and **Qt 6.12+**.

```
aqt install-qt linux desktop 6.12.0 linux_gcc_64 -O ~/Qt
# add to your shell profile:
export QtDir=~/Qt/6.12.0/gcc_64
alias maestro-run='LD_LIBRARY_PATH=$QtDir/lib dotnet run --project Maestro.Desktop'
```

Then run `maestro-run` from `maestro/`. Qt Bridge for C# comes from NuGet, so there's nothing to install for it.
