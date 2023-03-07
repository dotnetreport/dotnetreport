using ReportBuilder.Web.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ReportBuilder.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {            
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            JobScheduler.Start();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (JobScheduler.WebAppRootUrl == "")
            {
                string uri = HttpContext.Current.Request.Url.AbsoluteUri;
                JobScheduler.WebAppRootUrl = uri.Substring(0, uri.IndexOf("/", 8) + 1);
            }
        }
    }
}
