$BuildNumber = $Env:BUILD_BUILDNUMBER
$SourcesPath = $Env:BUILD_SOURCESDIRECTORY

$FilePath = "$SourcesPath\src\External\GlobalAssemblyInfo.cs"
$Content = Get-Content $FilePath

Write-Host ""
Write-Host "Replacing assembly version with build number: $BuildNumber"
$Content = $Content.Replace("1.0.0.0", $BuildNumber)

Write-Host "Saving GlobalAssemblyInfo.cs to $FilePath"
$Content | Out-File -FilePath $FilePath
