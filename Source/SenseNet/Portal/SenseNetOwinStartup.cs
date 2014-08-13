using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using SenseNet.Diagnostics;
using Microsoft.AspNet.SignalR;
using SenseNet.ContentRepository.Storage.Data;

[assembly: OwinStartup(typeof(SenseNet.Portal.SenseNetOwinStartup))]

namespace SenseNet.Portal
{
    public class SenseNetOwinStartup
    {
        public void Configuration(IAppBuilder app)
        {
            // create SignalR SQL tables only if the NLB option is enabled
            if (RepositoryConfiguration.SignalRSqlEnabled)
                GlobalHost.DependencyResolver.UseSqlServer(RepositoryConfiguration.SignalRDatabaseConnectionString);

            app.MapSignalR();
        }
    }
}
