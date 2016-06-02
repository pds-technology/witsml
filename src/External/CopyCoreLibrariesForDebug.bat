echo Copying Core libraries for debugging...

pushd "%~dp0..\packages"

for /D %%f in (PDS.Framework.20*) do (
	@if exist %%f\lib\net46 (
	    copy "%~dp0..\..\..\witsml\src\Framework\bin\Debug\PDS.Framework.*" %%f\lib\net46
	)
)

for /D %%f in (PDS.Framework.Web.20*) do (
	@if exist %%f\lib\net46 (
	    copy "%~dp0..\..\..\witsml\src\Framework.Web\bin\Debug\PDS.Framework.Web.*" %%f\lib\net46
	)
)

for /D %%f in (PDS.Witsml.20*) do (
	@if exist %%f\lib\net46 (
	    copy "%~dp0..\..\..\witsml\src\Witsml\bin\Debug\PDS.Witsml.*" %%f\lib\net46
	)
)

for /D %%f in (PDS.Witsml.Server.20*) do (
	@if exist %%f\lib\net46 (
	    copy "%~dp0..\..\..\witsml\src\Witsml.Server\bin\Debug\PDS.Witsml.Server.*" %%f\lib\net46
	)
)

for /D %%f in (PDS.Witsml.Server.IntegrationTest.20*) do (
	@if exist %%f\lib\net46 (
	    copy "%~dp0..\..\..\witsml\src\Witsml.Server.IntegrationTest\bin\Debug\PDS.Witsml.Server.IntegrationTest.*" %%f\lib\net46
	)
)

for /D %%f in (PDS.Witsml.Server.Web.20*) do (
	@if exist %%f\lib\net46 (
	    copy "%%~dp0..\..\..\witsml\src\Witsml.Server.Web\bin\Debug\PDS.Witsml.Server.Web.*" %%f\lib\net46
	)
)

popd

Echo.%1 | findstr /C:"IntegrationTest">nul && (
	mkdir %1TestData
	copy "%~dp0..\..\..\witsml\src\Witsml.Server.IntegrationTest\bin\Debug\TestData\*.*" %1TestData
) || (
    Echo.
)
