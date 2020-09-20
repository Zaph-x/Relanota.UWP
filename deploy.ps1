Set-Location .\FrontEnd\
if ((Test-Path ~\Desktop\Relanote\backup) -eq $false) {
    mkdir ~\Desktop\Relanote\backup
}
if (Test-Path ~\Desktop\Relanote\notes.db) {
    Copy-Item ~\Desktop\Relanote\notes.db "~\Desktop\Relanote\backup\notes_backup_$(Get-Date -Format "ddMMyyyy_HHmmss").db"
}

dotnet publish -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:DebugType=None /p:DebugSymbols=false
Copy-Item .\bin\Debug\netcoreapp3.1\win-x64\publish\FrontEnd.exe ~\Desktop\Relanote\Relanote.exe
Copy-Item .\bin\Debug\netcoreapp3.1\win-x64\publish\FrontEnd.pdb ~\Desktop\Relanote\
Set-Location ..