$pkgDir = "./appx_package"
if (Test-Path $pkgDir) { Remove-Item -Path $pkgDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $pkgDir
New-Item -ItemType Directory -Force -Path "$pkgDir/Assets"

Copy-Item -Path "./publish/TavrynexoAntiSpy.exe" -Destination "$pkgDir/TavrynexoAntiSpy.exe"
Copy-Item -Path "./AppxManifest.xml" -Destination "$pkgDir/AppxManifest.xml"
Copy-Item -Path "./Assets/*" -Destination "$pkgDir/Assets" -Force

# Locate makeappx.exe and makepri.exe from Windows SDK
$sdkDir = (Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" -Filter "makeappx.exe" -Recurse | Where-Object { $_.FullName -like "*x64*" } | Select-Object -First 1).DirectoryName
$makeAppx = Join-Path $sdkDir "makeappx.exe"
$makePri = Join-Path $sdkDir "makepri.exe"
Write-Host "Using MakeAppx: $makeAppx"
Write-Host "Using MakePri: $makePri"

# Generate resources.pri required by Microsoft Store Partner Center
if (Test-Path $makePri) {
    & $makePri createconfig /cf "$pkgDir/priconfig.xml" /dq en-US /pv 10.0.0 /o
    & $makePri new /pr "$pkgDir" /cf "$pkgDir/priconfig.xml" /of "$pkgDir/resources.pri" /o
    Remove-Item -Path "$pkgDir/priconfig.xml" -Force -ErrorAction SilentlyContinue
}

New-Item -ItemType Directory -Force -Path "./store_packages"
& $makeAppx pack /d $pkgDir /p ./store_packages/TavrynexoAntiSpy.msix /o
& $makeAppx pack /d $pkgDir /p ./store_packages/TavrynexoAntiSpy.appx /o
