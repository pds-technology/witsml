$versionPattern1 = "// PDS\.Framework\, (\d|\w|\.)*"

$versionPattern2 = "// PDS\.Witsml\, (\d|\w|\.)*"

$versionPattern3 = "// PDS\.Witsml\.Server\, (\d|\w|\.)*"

$copyrightPattern1 = "// Copyright (\d)* Petrotechnical Data Systems"

$copyrightPattern2 = "// Copymessageht (\d)* Petrotechnical Data Systems"

$year = "2017"
$version = "$year.1"

ls (Split-Path $dte.Solution.FileName -Parent) -Recurse -include *.cs |
  foreach {
    $content = cat $_.FullName | Out-String
    $origContent = $content

    $content = $content -replace $versionPattern1, "// PDS.Framework, $version"
    $content = $content -replace $versionPattern2, "// PDS.Witsml, $version"
    $content = $content -replace $versionPattern3, "// PDS.Witsml.Server, $version"
    $content = $content -replace $copyrightPattern1, "// Copyright $year Petrotechnical Data Systems"
    $content = $content -replace $copyrightPattern2, "// Copyright $year Petrotechnical Data Systems"

    if ($origContent -ne $content)
    {	
        $content.Trim() | out-file -encoding "UTF8" $_.FullName
        write-host Updated $_.Name
    }		    
}