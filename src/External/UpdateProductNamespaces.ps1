$namePattern1 = "PDS\.Framework"
$namePattern2 = "PDS\.Witsml\.Server\.Web"
$namePattern3 = "PDS\.Witsml\.Server\.Jobs"
$namePattern4 = "PDS\.Witsml\.Server"
$namePattern5 = "PDS\.Witsml"

ls (Split-Path $dte.Solution.FileName -Parent) -Recurse -include *.cs, *.csproj, *.config |
  foreach {
    $content = cat $_.FullName | Out-String
    $origContent = $content

    $content = $content -replace $namePattern1, "PDS.WITSMLstudio.Framework"
    $content = $content -replace $namePattern2, "PDS.WITSMLstudio.Store.Web"
    $content = $content -replace $namePattern3, "PDS.WITSMLstudio.Store.Jobs"
    $content = $content -replace $namePattern4, "PDS.WITSMLstudio.Store.Core"
    $content = $content -replace $namePattern5, "PDS.WITSMLstudio.Core"

    if ($origContent -ne $content)
    {	
        $content.Trim() | out-file -encoding "UTF8" $_.FullName
        write-host Updated $_.Name
    }		    
}
