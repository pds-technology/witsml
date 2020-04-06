# Script must be executed from Visual Studio's Package Manager Console

$hintPathPattern = @"
<HintPath>(\d|\w|\s|\.|\\)*packages
"@

$importPathPattern = @"
<Import Project="(\d|\w|\s|\.|\\)*packages
"@

$existsPathPattern = @"
Exists\('(\d|\w|\s|\.|\\)*packages
"@

$formatPathPattern = @"
Format\('\$\(ErrorText\)', '(\d|\w|\s|\.|\\)*packages
"@

$analyzerPathPattern = @"
<Analyzer Include="(\d|\w|\s|\.|\\)*packages
"@

ls (Split-Path $PSScriptRoot -Parent) -Recurse -include *.csproj, *.sln, *.fsproj, *.vbproj |
  foreach {
    $content = cat $_.FullName | Out-String
    $origContent = $content

    $content = $content -replace $hintPathPattern, "<HintPath>`$(SolutionDir)\packages"
    $content = $content -replace $importPathPattern, "<Import Project=""`$(SolutionDir)\packages"
    $content = $content -replace $analyzerPathPattern, "<Analyzer Include=""`$(SolutionDir)\packages"
    $content = $content -replace $existsPathPattern, "Exists('`$(SolutionDir)\packages"
    $content = $content -replace $formatPathPattern, "Format('`$(ErrorText)', '`$(SolutionDir)\packages"

    if ($origContent -ne $content)
    {	
        $content.Trim() | out-file -encoding "UTF8" $_.FullName
        write-host Updated $_.Name
    }		    
}
