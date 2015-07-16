param($installPath, $toolsPath, $package,$project)

notepad.exe

$installPath
$toolsPath
$package
$project

invoke-item $toolsPath
Read-Host