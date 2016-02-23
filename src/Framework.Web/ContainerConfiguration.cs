using System;
using System.IO;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace PDS.Framework.Web
{
    public static class ContainerConfiguration
    {
        public static void Register(string assemblyPath)
        {
            var container = ContainerFactory.Create(assemblyPath);
            var resolver = new DependencyResolver(container);

            // Install dependency resolver for MVC
            System.Web.Mvc.DependencyResolver.SetResolver(resolver);

            // Install dependency resolver for Web API
            GlobalConfiguration.Configuration.DependencyResolver = resolver;

            // Install custom Web API controller factory
            ControllerBuilder.Current.SetControllerFactory(new ControllerFactory(container));
        }

        public static string MapWorkingDirectory(string path)
        {
            if (HttpContext.Current == null)
                return Path.Combine(Environment.CurrentDirectory, path);

            return HttpContext.Current.Server.MapPath(Path.Combine("~/bin", path));
        }
    }
}
