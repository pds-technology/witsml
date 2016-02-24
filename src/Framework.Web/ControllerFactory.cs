using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace PDS.Framework.Web
{
    /// <summary>
    /// Represents a controller factory with knowledge of the composition container.
    /// </summary>
    /// <seealso cref="System.Web.Mvc.DefaultControllerFactory" />
    public class ControllerFactory : DefaultControllerFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public ControllerFactory(IContainer container)
        {
            Container = container;
        }

        /// <summary>
        /// Gets the composition container.
        /// </summary>
        /// <value>The container.</value>
        public IContainer Container { get; private set; }

        /// <summary>
        /// Retrieves the controller instance for the specified request context and controller type.
        /// </summary>
        /// <param name="requestContext">The context of the HTTP request, which includes the HTTP context and route data.</param>
        /// <param name="controllerType">The type of the controller.</param>
        /// <returns>The controller instance.</returns>
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
