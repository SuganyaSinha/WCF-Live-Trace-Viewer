using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml.Linq;

namespace WcfLiveTraceViewer
{
    static class WcfProcessInfo
    {
        //This method geta the processes having System.ServiceModel assembly loaded
        public static List<AppData> GetRunningProcessesHavingWcf()
        {
            List<AppData> appDataList = new List<AppData>();
            //Get the running processes in the system
            Process[] runningProcesses = Process.GetProcesses();

            // Get the assemblies loaded in each process
            foreach (Process process in runningProcesses)
            {
                // Ignore the modules in w3wp because it is already taken care 
                // by GetWebsitesHavingWcf() method
                if (process.ProcessName.Contains("w3wp"))
                    continue;

                ProcessModuleCollection modules = null;
                try
                {
                    modules = process.Modules;
                }
                catch
                {
                    continue;
                }

                foreach (ProcessModule module in modules)
                {
                    if (module.ModuleName.Contains("System.ServiceModel"))
                    {
                        AppData pd = new AppData(process.ProcessName, process.MainModule.FileName, process.MainModule.ModuleName + ".config");
                        if (System.IO.File.Exists(process.MainModule.FileName + ".config"))
                        {
                            appDataList.Add(pd);
                            break;
                        }
                    }
                }
            }
            return appDataList;
        }

        public static List<AppData> GetWebsitesHavingWcf()
        {
            List<AppData> appDataList = new List<AppData>();
            Microsoft.Web.Administration.ServerManager manager = new Microsoft.Web.Administration.ServerManager();

            try
            {
                foreach (var site in manager.Sites)
                {
                    foreach (var application in site.Applications)
                    {
                        foreach (var vd in application.VirtualDirectories)
                        {
                            string configPath = vd.PhysicalPath + @"\web.config";
                            if (System.IO.File.Exists(configPath))
                            {
                                XElement doc = XElement.Load(configPath);
                                var serviceModelNode = doc.Descendants("system.serviceModel");
                                if (serviceModelNode.Count() > 0)
                                {
                                    AppData pd = new AppData(application.ToString(), vd.PhysicalPath, "web.config");
                                    appDataList.Add(pd);
                                }
                            }


                        }
                    }
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
            

            return appDataList;
        }
    }
}
