using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using Microsoft.Web.XmlTransform;
using System.Configuration;
using System.Xml.Linq;

namespace WcfLiveTraceViewer
{

    public partial class TraceConfigure : Window
    {
        List<AppData> wcfConfigPaths = new List<AppData>();
        bool browseMode = false;
        string selectedConfigFile = "";
        int freePortNumer = -1;

        public TraceConfigure()
        {
            InitializeComponent();
            txtMessage.Text = "";
            try
            {
                wcfConfigPaths = WcfProcessInfo.GetRunningProcessesHavingWcf();
                wcfConfigPaths.AddRange(WcfProcessInfo.GetWebsitesHavingWcf());
            }
            catch (UnauthorizedAccessException unAuthorizedEx)
            {
                txtMessage.Text = "Unauthorized to enumerate the websites. Please run the WCF Live Trace Viewer as administrator or select a configuration file";
            }
            catch(Exception ex)
            {
                txtMessage.Text = ex.Message;
            }

            view.ItemsSource = wcfConfigPaths;
        }

        private void btnEnableTracing_Click(object sender, RoutedEventArgs e)
        {
            txtMessage.Text = "";
           
            if (browseMode == true)
            {
                // Fetch the selected path and file
                int location = selectedConfigFile.LastIndexOf(@"\");
                string path = selectedConfigFile.Substring(0, location + 1);
                string fileName = selectedConfigFile.Substring(location + 1, (selectedConfigFile.Length - (location + 1)));

                if (fileName.EndsWith(".config",StringComparison.OrdinalIgnoreCase))
                {
                    if (EnableTracing(path, fileName))
                    {
                        browseMode = false;
                        if (string.Equals(fileName, "web.config", StringComparison.OrdinalIgnoreCase))
                        {
                            txtMessage.Text = "Tracing has been enabled for " + selectedConfigFile + ". Please access the website associated with this configuration file for the configuration changes to get applied";
                        }
                        else
                        {
                            txtMessage.Text = "Tracing has been enabled for " + selectedConfigFile + ".Please restart the process associated with this configuration file for the configuration changes to get applied";
                        }
                    }
                }
                else
                {
                    txtMessage.Text = "Please select a configuration file";
                }

            }
            else
            {
                // Get the selected item
                var items = (from data in wcfConfigPaths.Where(item => item.Selected == true)
                             select (data)).ToList();

                if (items.Count == 0)
                {
                    txtMessage.Text = "Please select a configuration file or process";
                }
                else
                {
                    string path = "";
                    if (string.Equals(items[0].ConfigName, "web.config", StringComparison.OrdinalIgnoreCase))
                    {
                        path = items[0].Path + @"\";
                    }
                    else
                    {
                        int location = items[0].Path.LastIndexOf(@"\");
                        path = items[0].Path.Substring(0, location + 1);
                    }

                    if (EnableTracing(path, items[0].ConfigName))
                    {
                        selectedConfigFile = path + items[0].ConfigName;
                        if (string.Equals(items[0].ConfigName, "web.config", StringComparison.OrdinalIgnoreCase))
                        {
                            txtMessage.Text = "Tracing has been enabled for " + selectedConfigFile + ". Please access the selected website for the configuration changes to get applied";
                        }
                        else
                        {
                            txtMessage.Text = "Tracing has been enabled for " + selectedConfigFile + ". Please restart the selected process for the configuration changes to get applied";
                        }
                    }

                }
            }
        }

        private bool EnableTracing(string path, string fileName)
        {
            string assemblyCopyPath = path;
            if (string.Equals(fileName, "web.config", StringComparison.OrdinalIgnoreCase))
            {
                assemblyCopyPath = path + @"bin\";
            }

            try
            {
                Utilities.CopyAssemblies(assemblyCopyPath);
                freePortNumer = Utilities.GetUnusedPortNumber();
                UpdateConfigFileToEnableTracing(path, fileName, freePortNumer.ToString());

                // Update traceViewer config file
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["signalrPortNumber"].Value = freePortNumer.ToString();
                config.AppSettings.Settings["tracingEnabled"].Value = "true";
                config.AppSettings.Settings["traceLocation"].Value = path + @"\" + fileName;
                config.Save();
                ConfigurationManager.RefreshSection("appSettings");
                return true;
            }
            catch(Exception ex)
            {
                if (Utilities.GetAppsettingsValue("displayErrors") == "True")
                    txtMessage.Text = "Error while enabling tracing: " + ex.Message;
                return false;
            }
            
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            txtMessage.Text = "";

            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectedConfigFile = dlg.FileName;
                txtMessage.Text = "The selected configuration file is : " + selectedConfigFile;
                browseMode = true;
            }
        }

        private void UpdateConfigFileToEnableTracing(string folder, string fileName, string portNumber)
        {
            string configFile = folder + @"\" + fileName;
  
            string configCopyFile = System.IO.Path.Combine(folder, fileName + "_copy.config");

            File.Copy(configFile, configCopyFile, true);

            //Load the source config file
            using (XmlTransformableDocument sourceFile = new XmlTransformableDocument())
            {
                sourceFile.PreserveWhitespace = true;
                sourceFile.Load(configFile);
                var appSettings = ConfigurationManager.AppSettings;

                // Load the transformation file
                using (XmlTransformation transformationFile = new XmlTransformation(appSettings["InstallTransformFile"]))
                {
                    bool success = transformationFile.Apply(sourceFile);
                    sourceFile.Save(configFile);
                }

            }

            // Add appsettings node for the signalRPort number
            XDocument document = XDocument.Load(configFile);
            var appSettingsNode = document.Element("configuration").Element("appSettings");
            if (appSettingsNode != null)
            {
                // appSettings node already exists. Check the existence of our add nodes before adding
                var appNodes = (from kk in appSettingsNode.Descendants("add").Attributes("key")
                                where kk.Value == "signalRPortNumber"
                                select kk).ToList();

                if (appNodes.Count == 0)
                {
                    // node does not exist. Add our node
                    XElement addElement = new XElement("add",
                                                       new XAttribute("key", "signalRPortNumber"),
                                                       new XAttribute("value", portNumber)
                                                       );
                    appSettingsNode.Add(addElement);
                }
                else
                {
                    // our key is already present. just update the port number
                    var node = appSettingsNode.Descendants("add").Where(e => e.Attribute("key").Value == "signalRPortNumber").FirstOrDefault();
                    if (node != null)
                        node.Attribute("value").Value = portNumber;
                }
            }
            else
            {
                // appSettings node does not exist
                XElement appSettingsElement = new XElement("appSettings",
                                                            new XElement("add",
                                                                       new XAttribute("key", "signalRPortNumber"),
                                                                       new XAttribute("value", portNumber))
                                                           );
                document.Element("configuration").Add(appSettingsElement);
            }

            document.Save(configFile);
        }

        private void btnDisableTracing_Click(object sender, RoutedEventArgs e)
        {
            txtMessage.Text = "";

            if (browseMode == true)
            {

                // Fetch the selected path and file
                int location = selectedConfigFile.LastIndexOf(@"\");
                string path = selectedConfigFile.Substring(0, location+1);
                string fileName = selectedConfigFile.Substring(location + 1, (selectedConfigFile.Length - (location + 1)));

                if (fileName.EndsWith(".config", StringComparison.OrdinalIgnoreCase))
                {
                    if (DisableTracing(path, fileName))
                    {
                        browseMode = false;
                        txtMessage.Text = "Tracing has been disabled for " + selectedConfigFile;
                    }
                }
                else
                {
                    txtMessage.Text = "Please select a configuration file";
                }

            }
            else
            {
               var items = (from data in wcfConfigPaths.Where(item => item.Selected == true)
                           select (data)).ToList();

                if (items.Count == 0)
                {
                    txtMessage.Text = "Please select a configuration file or process";
                }
                else
                {
                    string path = "";
                    if (string.Equals(items[0].ConfigName, "web.config", StringComparison.OrdinalIgnoreCase))
                    {
                        path = items[0].Path + @"\";
                    }
                    else
                    {
                        int location = items[0].Path.LastIndexOf(@"\");
                        path = items[0].Path.Substring(0, location + 1);
                    }

                    if (DisableTracing(path, items[0].ConfigName))
                    {
                        selectedConfigFile = path + items[0].ConfigName;
                        txtMessage.Text = "Tracing has been disabled for " + selectedConfigFile;
                    }
                }
            }
        }

        private bool DisableTracing(string path, string fileName)
        {
            string assemblyCopyPath = path;
            if (string.Equals(fileName, "web.config", StringComparison.OrdinalIgnoreCase))
            {
                assemblyCopyPath = path + @"bin\";
            }

            try
            {
                UpdateConfigFileToDisableTracing(path, fileName);
                Utilities.RemoveAssemblies(assemblyCopyPath);

                // Update traceViewer config file
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["signalrPortNumber"].Value = "-1";
                config.AppSettings.Settings["tracingEnabled"].Value = "false";
                config.AppSettings.Settings["traceLocation"].Value = "";
                config.Save();
                ConfigurationManager.RefreshSection("appSettings");
                return true;
            }
            catch(Exception ex)
            {
                if (Utilities.GetAppsettingsValue("displayErrors") == "True")
                    txtMessage.Text = "Error while disabling tracing: " + ex.Message;
                return false;
            }

        }

        // used transformation to update the selected config file for disabling tracing
        private void UpdateConfigFileToDisableTracing(string folder, string fileName)
        {
            string configFile = folder + @"\" + fileName;
            string configCopyFile = System.IO.Path.Combine(folder, fileName + "_copy.config");

            // Backup before copying
            File.Copy(configFile, configCopyFile, true);

            //Load the source config file
            using (XmlTransformableDocument sourceFile = new XmlTransformableDocument())
            {
                sourceFile.PreserveWhitespace = true;
                sourceFile.Load(configFile);
                var appSettings = ConfigurationManager.AppSettings;

                // Load the transformation file
                using (XmlTransformation transformationFile = new XmlTransformation(appSettings["UninstallTransformFile"]))
                {
                    bool success = transformationFile.Apply(sourceFile);
                    sourceFile.Save(configFile);
                }
            }
        }
  
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RadioButton_Clicked(object sender, RoutedEventArgs e)
        {
            browseMode = false;
            txtMessage.Text = "";
        }


    }
}
