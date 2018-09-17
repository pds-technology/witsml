$BuildNumber = $Env:BUILD_BUILDNUMBER
$SourcesPath = $Env:BUILD_SOURCESDIRECTORY

Get-ChildItem -Path "$SourcesPath" -Filter "GlobalAssemblyInfo.cs" -File -Recurse -ErrorAction SilentlyContinue -Force |
    ForEach-Object {
        $FilePath = $_.FullName
        $Content = Get-Content $FilePath

        Write-Host ""
        Write-Host "Replacing assembly version with build number: $BuildNumber"
        $Content = $Content -replace "1.0.0.0", "$BuildNumber"

        Write-Host "Saving GlobalAssemblyInfo.cs to $FilePath"
        $Content | Out-File -FilePath $FilePath
    }
