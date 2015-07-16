using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Owin;

namespace CustomTraceListener
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR<ListenerConnection>("/listener", new ConnectionConfiguration()
            {
                EnableJSONP = true,
                
            });
        }
    }
}
