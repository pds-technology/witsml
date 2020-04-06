//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using log4net.Config;
using Hangfire.Mongo;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Framework.Web;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data;
using PDS.WITSMLstudio.Store.Jobs.Configuration;
using PDS.WITSMLstudio.Store.Controllers;

namespace PDS.WITSMLstudio.Store
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            System.Web.Http.GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            XmlConfigurator.ConfigureAndWatch(new FileInfo(Server.MapPath("~/log4net.config")));
            ContainerConfiguration.Register(Server.MapPath("~/bin"));

            if (string.IsNullOrWhiteSpace(WitsmlSettings.OverrideServerVersion))
                WitsmlSettings.OverrideServerVersion = typeof(EtpController).GetAssemblyVersion();

            var container = (IContainer)System.Web.Http.GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IContainer));
            var databaseProvider = container.Resolve<IDatabaseProvider>();

            // pre-init IWitsmlStore dependencies
            var store = container.Resolve<IWitsmlStore>();
            store.WMLS_GetCap(new WMLS_GetCapRequest(OptionsIn.DataVersion.Version141));

            Task.Run(async () =>
            {
                // Wait before initializing Hangfire to give server time to warm up
                await Task.Delay(WitsmlSettings.ChangeDetectionPeriod * 1000);

                var storageOptions = new MongoStorageOptions()
                {
                    MigrationOptions = new MongoMigrationOptions()
                    {
                        Strategy = MongoMigrationStrategy.Migrate,
                        BackupStrategy = MongoBackupStrategy.None,
                    }
                };

                // Configure and register Hangfire jobs
                Hangfire.GlobalConfiguration.Configuration.UseMongoStorage(databaseProvider.ConnectionString, storageOptions);
                HangfireConfig.Register(container);
            });
        }

        protected void Application_End(object sender, EventArgs e)
        {
            HangfireConfig.Unregister();
        }
    }
}
