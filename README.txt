PS3 PKG builder utility for the Rock Band 3 Deluxe mod.
https://github.com/hmxmilohax/rock-band-3-deluxe

If you want to build PKGs yourself, see the library/utility here:
https://github.com/InvoxiPlayGames/SCEllSharp

Building requires the .NET 8.0 SDK installed, as well as the necessary build
tools for NativeAOT compilation. (MSVC via VS, Xcode CLI tools, or Clang)

1. Git clone with submodules.
   git clone --recursive https://github.com/InvoxiPlayGames/RB3DXBuildPkgPS3

2. Restore NuGet packages using the dotnet CLI
   dotnet restore

3. Build a managed .NET executable
   dotnet build
   -- OR --
   Publish for NativeAOT
   dotnet publish -r win-x64 -c Release
   (Replace "win-x64" with "linux-x64" or "macos-arm64" depending on platform)
