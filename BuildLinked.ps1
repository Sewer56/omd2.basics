# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/omd2.basics/*" -Force -Recurse
dotnet publish "./src/omd2.basics.csproj" -c Release -o "$env:RELOADEDIIMODS/omd2.basics" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location