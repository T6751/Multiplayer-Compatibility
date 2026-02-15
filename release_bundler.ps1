# PowerShell script for creating release package on Windows
# Equivalent to release_bundler.sh

$ErrorActionPreference = "Stop"

Write-Host "Building Release configuration..."
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Cleaning up old release directory..."
if (Test-Path "Multiplayer-Compatibility") {
    Remove-Item -Recurse -Force "Multiplayer-Compatibility"
}

Write-Host "Creating release directory..."
New-Item -ItemType Directory -Path "Multiplayer-Compatibility" | Out-Null

Write-Host "Copying files..."
Copy-Item -Recurse "About" "Multiplayer-Compatibility\"
Copy-Item -Recurse "Assemblies" "Multiplayer-Compatibility\"
Copy-Item -Recurse "Referenced" "Multiplayer-Compatibility\"
Copy-Item -Recurse "Languages" "Multiplayer-Compatibility\"

Write-Host "Creating zip archive..."
if (Test-Path "Multiplayer-Compatibility.zip") {
    Remove-Item -Force "Multiplayer-Compatibility.zip"
}

Compress-Archive -Path "Multiplayer-Compatibility" -DestinationPath "Multiplayer-Compatibility.zip" -Force

Write-Host "Ok, $PWD\Multiplayer-Compatibility.zip ready"