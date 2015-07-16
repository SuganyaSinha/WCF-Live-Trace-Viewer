using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using System.Diagnostics;
using Microsoft.AspNet.SignalR;
using System.Xml.XPath;
using System.Xml;
using System.Globalization;
using System.Reflection;
using System.Configuration;

namespace CustomTraceListener
{
    public class SignalRTraceListener : System.Diagnostics.TraceListener
    {
        private static IDisposable _signalRServer;
        static SignalRTraceListener()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string signalrPortNumber = appSettings["signalRPortNumber"].ToString();
            if (signalrPortNumber != null)
            {
                string url = "http://localhost:" + signalrPortNumber + "/";
                try
                {
                    _signalRServer = WebApp.Start<Startup>(url);
                }
                catch (Exception ex)
                {

                }
            }
 
        }

        public override void Write(string message)
        {
            //if (newmessage != null)
            //{ 
            //    newmessage(this, )
            //}
        }

        public override void TraceData(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, object data)
        {
            // Get ActivityId from the eventCache. 
            // ActivityId is an internal property. Using reflection to get its value
            string activityID = "";

            if (eventCache != null)
            {
                PropertyInfo propInfo = null;
                Type objType = eventCache.GetType();
                propInfo = objType.GetProperty("ActivityId", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic);
                if (propInfo != null)
                {
                    object value = propInfo.GetValue(eventCache, null);
                    if (value != null)
                        activityID = value.ToString();
                }
            }

            Process p = Process.GetProcessById(eventCache.ProcessId);
            // Header message that is added by this listener
            string msgHeader = string.Format("<HeaderAddedByListener time=\"{0}\" ActivityId=\"{1}\" ProcessId=\"{2}\">", eventCache.DateTime.ToString(), activityID, p.ProcessName);
            
            // Read the message received from wcf
            XPathNavigator xPathNavigator = data as XPathNavigator;
            if (xPathNavigator == null)
            {
                return;
            }

            XmlTextWriter xmlWriter;
            StringBuilder msgReceived = new StringBuilder();
            xmlWriter = new XmlTextWriter(new System.IO.StringWriter(msgReceived, CultureInfo.CurrentCulture));

            xPathNavigator.MoveToRoot();
            xmlWriter.WriteNode(xPathNavigator, false);

            // Broadcast the msg
            string msgToSend = string.Format("{0}{1}</HeaderAddedByListener>", msgHeader, msgReceived.ToString());
            GlobalHost.ConnectionManager.GetConnectionContext<ListenerConnection>().Connection.Broadcast(msgToSend);

            //base.TraceData(eventCache, source, eventType, id, data);

            if (xmlWriter != null)
            {
                xmlWriter.Close();
            }
            xmlWriter = null;
            msgReceived = null;
        }

        public override void TraceData(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, params object[] data)
        {
            
            //var msg = (data as System.Xml.XPath.XPathNavigator).OuterXml.ToString();
            //GlobalHost.ConnectionManager.GetConnectionContext<ListenerConnection>().Connection.Broadcast(msg);
           // base.TraceData(eventCache, source, eventType, id, data);
        }

        public override void WriteLine(string message)
        {
            //throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_signalRServer != null)
            {
                _signalRServer.Dispose();
                _signalRServer = null;
            }

        }
    }
}
