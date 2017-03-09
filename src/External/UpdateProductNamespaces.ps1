$pattern01 = "// PDS\.Framework\,"
$pattern02 = "// PDS\.Witsml\,"
$pattern03 = "// PDS\.Witsml.Server\,"
$pattern04 = "// PDS\.Witsml.Studio\,"
$pattern05 = "// PDS\.Witsml.Utility\,"

$pattern06 = "\(\""PDS\.Framework\""\)"
$pattern07 = "\(\""PDS\.Witsml\""\)"
$pattern08 = "\(\""PDS\.Witsml.Server\""\)"
$pattern09 = "\(\""PDS\.Witsml.Studio\""\)"
$pattern10 = "\(\""PDS\.Witsml.Utility\""\)"

$pattern11 = "PDS\.Framework"
$pattern12 = "PDS\.Witsml\.Utility"
$pattern13 = "PDS\.Witsml\.Studio"
$pattern14 = "PDS\.Witsml\.Server"
$pattern15 = "PDS\.Witsml\.Web"
$pattern16 = "PDS\.Witsml"

ls (Split-Path $dte.Solution.FileName -Parent) -Recurse -include *.cs, *.csproj, *.config, *.settings, *.nuspec |
  foreach {
    $content = cat $_.FullName | Out-String
    $origContent = $content

    $content = $content -creplace $pattern01, "// PDS WITSMLstudio Framework,"
    $content = $content -creplace $pattern02, "// PDS WITSMLstudio Core,"
    $content = $content -creplace $pattern03, "// PDS WITSMLstudio Store,"
    $content = $content -creplace $pattern04, "// PDS WITSMLstudio Desktop,"
    $content = $content -creplace $pattern05, "// PDS WITSMLstudio Utility,"

    $content = $content -creplace $pattern06, "(""PDS WITSMLstudio Framework"")"
    $content = $content -creplace $pattern07, "(""PDS WITSMLstudio Core"")"
    $content = $content -creplace $pattern08, "(""PDS WITSMLstudio Store"")"
    $content = $content -creplace $pattern09, "(""PDS WITSMLstudio Desktop"")"
    $content = $content -creplace $pattern10, "(""PDS WITSMLstudio Utility"")"

    $content = $content -creplace $pattern11, "PDS.WITSMLstudio.Framework"
    $content = $content -creplace $pattern12, "PDS.WITSMLstudio.Utility"
    $content = $content -creplace $pattern13, "PDS.WITSMLstudio.Desktop"
    $content = $content -creplace $pattern14, "PDS.WITSMLstudio.Store"
    $content = $content -creplace $pattern15, "PDS.WITSMLstudio.Store"
    $content = $content -creplace $pattern16, "PDS.WITSMLstudio"

    if ($origContent -ne $content)
    {	
        $content.Trim() | out-file -encoding "UTF8" $_.FullName
        write-host Updated $_.Name
    }		    
}
