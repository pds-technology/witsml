using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace PDS.Framework.Web
{
    public class ControllerFactory : DefaultControllerFactory
    {
        public ControllerFactory(IContainer container)
        {
            Container = container;
        }

        public IContainer Container { get; private set; }

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            var controller = Container.Resolve(controllerType) as IController;

            if (controller == null)
            {
                controller = base.GetControllerInstance(requestContext, controllerType);
                Container.BuildUp(controller);
            }

            return controller;
        }
    }
}
