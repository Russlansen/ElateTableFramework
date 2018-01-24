using ElateTableFramework.Binders;
using System.Web.Mvc;
using System.Web.Routing;

namespace TestApplication
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            ModelBinders.Binders.Add(typeof(double), new DoubleTypeBinder());
            ModelBinders.Binders.Add(typeof(double?), new DoubleTypeBinder());
        }
    }
}
