Set-Location .\FrontEnd\
dotnet publish -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:DebugType=None /p:DebugSymbols=false
Copy-Item .\bin\Debug\netcoreapp3.1\win-x64\publish\FrontEnd.exe ~\Desktop\Relanote\Relanote.exe
Copy-Item .\bin\Debug\netcoreapp3.1\win-x64\publish\FrontEnd.pdb ~\Desktop\Relanote\
Set-Location ..