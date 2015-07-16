using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using System.Net;
using System.Net.NetworkInformation;

namespace WcfLiveTraceViewer
{
    static class Utilities
    {
        public static void CopyAssemblies(string targetLocation)
        {

            // Fetch the folder from where assemblies need to be copied
            var appsettings = ConfigurationManager.AppSettings;
            string assemblyLocation = appsettings["AssemblyLocation"];

            if (assemblyLocation != null)
            {
                DirectoryInfo sourceFolder = new DirectoryInfo(assemblyLocation);

                // Copy all the files in the sourceFolder to the targetLocation
                foreach (var sourceFile in sourceFolder.GetFiles())
                {
                    string targetFile = targetLocation + sourceFile.Name;
                    File.Copy(sourceFile.FullName, targetFile, true);
                }
            }
        }

        public static int GetUnusedPortNumber()
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();

            List<int> usedPorts = tcpEndPoints.Select(p => p.Port).ToList<int>();

            int unusedPort =-1;

            for (int port = 43124; port < 44320; port++)
            //for (int port = 9000; port < 9005; port++)
            {
                if (!usedPorts.Contains(port))
                {
                    unusedPort = port;
                    break;
                }
            }

            return unusedPort;
        }

        public static string GetAppsettingsValue(string key)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string value = appSettings[key].ToString();
            if (value == null)
                return "";
            else
                return value;
        }

        public static void RemoveAssemblies(string path)
        {
            var appSettings = ConfigurationManager.AppSettings;
            List<string> assemblyList = new List<string>();

            // Get the list of assemblies to be removed from the app.config file
            string assemblyNames = appSettings["AssemblyNames"];
            assemblyList = assemblyNames.Split(',').ToList<string>();

            // locate the assemblies and delete
            foreach (var assemblyName in assemblyList)
            {
                string fileName = path +  assemblyName + ".dll";

                if (File.Exists(fileName))
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception ex)
                    {
                        //txtMessage.Text = ex.Message;
                        throw ex;
                    }

                }
            }
        }
    }
}
