echo Copying Core libraries for debugging...

pushd "%~dp0..\packages\PDS.Framework.*\lib\net46\"
copy "%~dp0..\..\..\witsml\src\Framework\bin\Debug\PDS.Framework.*"
popd

pushd "%~dp0..\packages\PDS.Framework.Web.*\lib\net46\"
copy "%~dp0..\..\..\witsml\src\Framework.Web\bin\Debug\PDS.Framework.Web.*"
popd

pushd "%~dp0..\packages\PDS.Witsml.*\lib\net46\"
copy "%~dp0..\..\..\witsml\src\Witsml\bin\Debug\PDS.Witsml.*"
popd

pushd "%~dp0..\packages\PDS.Witsml.Server.*\lib\net46\"
copy "%~dp0..\..\..\witsml\src\Witsml.Server\bin\Debug\PDS.Witsml.Server.*"
popd

pushd "%~dp0..\packages\PDS.Witsml.Server.IntegrationTest.*\lib\net46\"
copy "%~dp0..\..\..\witsml\src\Witsml.Server.IntegrationTest\bin\Debug\PDS.Witsml.Server.IntegrationTest.*"
popd

pushd "%~dp0..\packages\PDS.Witsml.Server.Web.*\lib\net46\"
copy "%~dp0..\..\..\witsml\src\Witsml.Server.Web\bin\Debug\PDS.Witsml.Server.Web.*"
popd
