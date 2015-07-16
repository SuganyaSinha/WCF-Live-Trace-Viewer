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
using Microsoft.AspNet.SignalR.Client;
using System.Xml.Linq;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml;
using mshtml;
using Microsoft.Web.XmlTransform;

namespace WcfLiveTraceViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class TraceViewer : Window
    {
        int traceId = 0;
        int messageId = 0;
        ObservableCollection<Item> messageData = new ObservableCollection<Item>();
        ObservableCollection<Item> traceData = new ObservableCollection<Item>();
        List<AppData> wcfConfigPaths = new List<AppData>();
        Item selectedMessageItem = null;
        Item selectedTraceItem = null;
        
        Connection connection;
        bool tracingEnabled = false;
        bool tracingStarted = false;
        bool connectionTimedOut = false;
        bool alreadyConnected = false;
        System.Timers.Timer signalrConnectionTimer = new System.Timers.Timer(30000);
      
        public TraceViewer()
        {
            InitializeComponent();
            TraceDataView.ItemsSource = traceData;
            MessageDataView.ItemsSource = messageData;
            signalrConnectionTimer.AutoReset = false;
            signalrConnectionTimer.Elapsed += signalrConnectionTimer_Elapsed;
            msgTxtBlock.Text = "";
        }

        void signalrConnectionTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            connectionTimedOut = true;
            signalrConnectionTimer.Stop();
            if (connection.State != ConnectionState.Connected)
            {
                App.Current.Dispatcher.Invoke(() => {
                    signalRConnectionProgressBar.Visibility = System.Windows.Visibility.Hidden;
                    msgTxtBlock.Text = "Connection timed out...";
                    Action.Content = "Start";
                });
                
                tracingStarted = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowState = WindowState.Maximized;
        }

        private void Action_Click(object sender, RoutedEventArgs e)
        {
            msgTxtBlock.Text = "";

            if (Action.Content.ToString() == "Start")
            {
                // Read the signalrPortNumber 
                // The signalrClient will be listening at this port number
                string signalrPortNumber = Utilities.GetAppsettingsValue("signalrPortNumber");
                string signalrServer = "http://localhost:" + signalrPortNumber + "/listener";

                if (tracingEnabled == true)
                {
                    signalRConnectionProgressBar.Visibility = System.Windows.Visibility.Visible;
                    msgTxtBlock.Text = "Connecting ...";
                    signalrConnectionTimer.Start();
                    connectionTimedOut = false;

                    if (connection == null)
                    {
                        connection = new Connection(signalrServer);
                        try
                        {
                            connection.Start();
                        }
                        catch(Exception ex)
                        {
                            if (Utilities.GetAppsettingsValue("displayErrors") == "True")
                                msgTxtBlock.Text = "Error while starting the listener: " + ex.Message;
                            return;
                        }
                        
                        connection.Received += data =>
                        {
                            //if (tracingEnabled == false)
                            //{
                            //    // This can happen if user has disabled tracing without stopping the trace
                            //    StopTracing();
                            //}

                            if (tracingStarted)
                            {

                                XElement traceXml = XElement.Parse(data.ToString());
                                System.Xml.Linq.XNode firstNode = traceXml.FirstNode;
                                if (firstNode.ToString().Contains("MessageLogTraceRecord"))
                                {
                                    AddMessageItem(traceXml);
                                }
                                else
                                {
                                    AddTraceItem(traceXml);
                                }
                            }

                        };
                        connection.Closed += connection_closed;
                        connection.StateChanged += connection_StateChanged;
                    }
                    else
                    {
                        connection.Start();
                    }

                    Action.Content = "Stop";
                    tracingStarted = true;
                }
                else
                {
                    // tracing is not enabled. Prompt the user to configure tracing
                    msgTxtBlock.Text = "Please select a WCF application to trace and then click Start";
                }
            }
            else
            {
                StopTracing();
            }
        }

        void connection_StateChanged(StateChange obj)
        {
            if (obj.NewState == ConnectionState.Connected)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (signalRConnectionProgressBar.Visibility == Visibility.Visible)
                        signalRConnectionProgressBar.Visibility = Visibility.Hidden;

                    msgTxtBlock.Text = "Listening for messages...";
                    Action.Content = "Stop";
                });

                signalrConnectionTimer.Stop();
                alreadyConnected = true;
                tracingStarted = true;
            }
        }

        private void StopTracing()
        {
            if (connection.State == ConnectionState.Connected)
            {
                connection.Stop();
            }

            Action.Content = "Start";
            tracingStarted = false;

            if (signalRConnectionProgressBar.Visibility == Visibility.Visible)
            {
                // user has cancelled the connection. ConnectionTimedOut is set to true so
                // reconnection does not happen again 
                connectionTimedOut = true;
                signalRConnectionProgressBar.Visibility = Visibility.Hidden;
                signalrConnectionTimer.Stop();

            }
        }

        private void connection_closed()
        {
            // Reconnect only when tracing is enabled
            if (tracingEnabled == true && connectionTimedOut == false && alreadyConnected == false)
            {
                int i = 0;
                while (i < 500)
                {
                    i++;
                }

                connection.Start();
            }

            if (alreadyConnected == true)
                alreadyConnected = false;

        }

        private void AddMessageItem(XElement traceXml)
        {
            try
            {
                XNamespace ns1 = "http://schemas.microsoft.com/2004/06/ServiceModel/Management/MessageTrace";
                XNamespace ns2 = "http://schemas.microsoft.com/2004/09/ServiceModel/Diagnostics";
                XNamespace ns3 = "http://schemas.microsoft.com/ws/2005/05/addressing/none";
                
                
                var ListenerActivityId = traceXml.Attribute("ActivityId");
                var processName = traceXml.Attribute("ProcessId");
                
                var requestXml = traceXml.Element(ns1 + "MessageLogTraceRecord");
                string inputXml = requestXml.ToString();

                DateTime time =(DateTime) requestXml.Attribute("Time");
                string source = requestXml.Attribute("Source").Value;
                var actionNode = requestXml.Descendants(ns3 + "Action");
                string action = "";


                if(actionNode.Count() > 0)
                {
                    action = actionNode.First().Value;
                    int index = action.LastIndexOf("/") + 1;
                    action = action.Substring(index, (action.Length) - index );
                }
    
                bool isRequest = true;
                bool hasError = false;

                if (inputXml.Contains("error") || inputXml.Contains("Exception") || inputXml.Contains("Fault"))
                    hasError = true;

                if (string.Equals("ServiceLevelReceiveRequest", source, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals("ServiceLevelSendRequest", source, StringComparison.OrdinalIgnoreCase)
                    )
                {
                    isRequest = true;
                }

                if (string.Equals("ServiceLevelSendReply", source, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals("ServiceLevelReceiveReply", source, StringComparison.OrdinalIgnoreCase)
                    )
                {
                    isRequest = false;
                }

                if (isRequest)
                {
                    var toNode = requestXml.Descendants(ns3 + "To");
                    string To = "";
                    {
                        if (toNode.Count() > 0)
                            To = toNode.First().Value;
                    }

                    messageId += 1;
                    Item traceItem = new Item("<?xml version=\"1.0\"?>" + inputXml, id: messageId, activityId: ListenerActivityId.Value, requestTime: time.TimeOfDay, action: action, processName: processName.Value, sentTo: To,source: source, hasError: hasError);
                    App.Current.Dispatcher.Invoke(() => { messageData.Add(traceItem); });
                }
                else
                {
                    var activityId = ListenerActivityId.Value;

                    var traceItem = messageData.SingleOrDefault(d => d.ActivityId == activityId);
                    if (traceItem != null)
                    {
                        traceItem.ResponseXml = "<?xml version=\"1.0\"?>" + inputXml;
                        traceItem.ResponseTime = time.TimeOfDay;
                        traceItem.HasError = hasError;
                    }
                }
            }
            catch(Exception e)
            {
                if (Utilities.GetAppsettingsValue("displayErrors") == "True")
                    msgTxtBlock.Text = "Error while adding the message items: " + e.Message;
            }
        }

        private void AddTraceItem(XElement traceXml)
        {
            try
            {
                string inputXml = traceXml.FirstNode.ToString();
                var ListenerActivityId = traceXml.Attribute("ActivityId");
                var processName = traceXml.Attribute("ProcessId");

                string description = "";
                XNamespace ns1 = "http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord";
                XNamespace ns2 = "http://schemas.microsoft.com/2006/08/ServiceModel/DictionaryTraceRecord";

                var actionNode = traceXml.Descendants(ns2 + "ActivityName");
                if (actionNode.Count() > 0)
                {
                    description = actionNode.First().Value;
                }
                else
                {
                    var descNode = traceXml.Descendants(ns1 + "Description");
                    if (descNode.Count() > 0)
                        description = descNode.First().Value;

                }
                
                bool hasError = false;

                if (inputXml.Contains("error") || inputXml.Contains("Exception") || inputXml.Contains("Fault"))
                    hasError = true;

                traceId += 1;
                Item traceItem = new Item(requestXml: "<?xml version=\"1.0\"?>" + inputXml, id: traceId, activityId: ListenerActivityId.Value, processName: processName.Value, hasError: hasError, descripton: description);
                App.Current.Dispatcher.Invoke(() => { traceData.Add(traceItem); });

            }
            catch (Exception e)
            {
                if (Utilities.GetAppsettingsValue("displayErrors") == "True")
                    msgTxtBlock.Text = "Error while adding trace items: " + e.Message;
            }
        }

        private void MessageDataView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            msgTxtBlock.Text = "";

            selectedMessageItem = ((WcfLiveTraceViewer.Item)((sender as System.Windows.Controls.ListView).SelectedItem));

            if (selectedMessageItem == null)
                return;

            try
            {
                DisplaySelectedItemData(selectedMessageItem.RequestXml, "Request");
                DisplaySelectedItemData(selectedMessageItem.ResponseXml, "Response");
            }
            catch(Exception ex)
            {
                if (Utilities.GetAppsettingsValue("displayErrors") == "True")
                    msgTxtBlock.Text = "Error while selecting the message item: " + ex.Message;
            }
        }

        private void DisplaySelectedItemData(string inputData, string type)
        {
            if (type == "Request")
            {
                XElement xml = XElement.Parse(inputData);
               // RequestHeaderXml.NavigateToString("<?xml version=\"1.0\"?>" + xml.FirstNode.ToString());

                XNamespace ns2 = "http://schemas.xmlsoap.org/soap/envelope/";
                var headerNode = xml.Descendants(ns2 + "Header");
                if (headerNode.Count() > 0)
                {
                    RequestHeaderXml.NavigateToString("<?xml version=\"1.0\"?>" + headerNode.FirstOrDefault().ToString());
                }

                var bodyNode = xml.Descendants(ns2 + "Body");
                if (bodyNode.Count() > 0)
                {
                    RequestEnvelopeXml.NavigateToString("<?xml version=\"1.0\"?>" + bodyNode.FirstOrDefault().ToString());
                }

                RequestRawXml.Text = inputData;
            }

            if (type == "Response")
            {
                if (inputData == null)
                {
                    return;
                }
                    
                XElement xml = XElement.Parse(inputData);
                XNamespace ns2 = "http://schemas.xmlsoap.org/soap/envelope/";
                var headerNode = xml.Descendants(ns2 + "Header");
                if (headerNode.Count() > 0)
                {
                    ResponseHeaderXml.NavigateToString("<?xml version=\"1.0\"?>" + headerNode.FirstOrDefault().ToString());
                }

                var bodyNode = xml.Descendants(ns2 + "Body");
                if (bodyNode.Count() > 0)
                {
                    ResponseEnvelopeXml.NavigateToString("<?xml version=\"1.0\"?>" + bodyNode.FirstOrDefault().ToString());
                }

                ResponseRawXml.Text = inputData;
            }

        }
        private void TraceDataView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            msgTxtBlock.Text = "";

            selectedTraceItem = ((WcfLiveTraceViewer.Item)((sender as System.Windows.Controls.ListView).SelectedItem));
            if (selectedTraceItem == null)
                return;

            try
            {
                traceXml.NavigateToString(selectedTraceItem.RequestXml);
            }
            catch (Exception ex)
            {
                if (Utilities.GetAppsettingsValue("displayErrors") == "True")
                    msgTxtBlock.Text = "Error while selecting trace item: " + ex.Message;
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            msgTxtBlock.Text = "";

            int selectedTabIndex = TabControl.SelectedIndex;
            Item selectedItem;
            if (selectedTabIndex == 0)
            {
                selectedItem = selectedMessageItem;
                if (selectedItem != null)
                {
                    int selectedItemIndex = messageData.IndexOf(selectedMessageItem);
                    messageData.RemoveAt(selectedItemIndex);

                    if (messageData.Count > 0)
                    {
                        // Set the previous item as selected
                        var previousItemIndex = selectedItemIndex > 0 ? selectedItemIndex - 1 : 0;
                        selectedMessageItem = messageData.ElementAt(previousItemIndex);
                        MessageDataView.SelectedItems.Clear();
                        MessageDataView.SelectedItems.Add(selectedMessageItem);
                    }
                    else
                    {
                        messageId = 0;
                        MessageDataView.SelectedItems.Clear();
                        selectedMessageItem = null;
                        InitializeBrowserControls();

                    }

                } 
                else
                {
                    msgTxtBlock.Text = "Please select an item to delete";
                }
            }
            else
            {
                selectedItem = selectedTraceItem;
                if (selectedItem != null)
                {
                    int selectedItemIndex = traceData.IndexOf(selectedItem);
                    traceData.RemoveAt(selectedItemIndex);

                    if (traceData.Count > 0)
                    {
                        var previousItemIndex = selectedItemIndex > 0 ? selectedItemIndex - 1 : 0;
                        selectedTraceItem = traceData.ElementAt(previousItemIndex);
                        TraceDataView.SelectedItems.Clear();
                        TraceDataView.SelectedItems.Add(selectedTraceItem);
                        traceXml.NavigateToString(selectedTraceItem.RequestXml);
                    }
                    else
                    {
                        traceId = 0;
                        selectedTraceItem = null;
                        TraceDataView.SelectedItems.Clear();
                        traceXml.NavigateToString(" ");
                    }

                }
                else
                {
                    msgTxtBlock.Text = "Please select an item to delete";
                }

            }
 
        }

        //If message tab is activce, then clear the messageData
        //If trace tab is activce, then clear the traceData
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            msgTxtBlock.Text = "";

            int selectedTabIndex = TabControl.SelectedIndex;
            if (selectedTabIndex == 0)
            {
                messageData.Clear();
                messageId = 0;
                selectedMessageItem = null;
                InitializeBrowserControls();

            }
            else
            {
                traceData.Clear();
                traceId = 0;
                selectedTraceItem = null;
                traceXml.NavigateToString(" ");
            }

        }

        // If message tab is active then serializes messageData to the disk:MessageLog.txt 
        // If trace tab is active then serializes traceData to the disk:TraceLog.txt 
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            msgTxtBlock.Text = "";

            int selectedTabIndex = TabControl.SelectedIndex;
            if (selectedTabIndex == 0)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = "MessageLog.txt";

                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(ObservableCollection<Item>));
                        using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName))
                        {
                            xs.Serialize(sw, messageData);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Utilities.GetAppsettingsValue("displayErrors") == "True")
                            msgTxtBlock.Text = "Error while saving the messages: " + ex.Message;
                    }

                }
            }
            else
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = "TraceLog.txt";

                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(ObservableCollection<Item>));
                        using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName))
                        {
                            xs.Serialize(sw, traceData);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Utilities.GetAppsettingsValue("displayErrors") == "True")
                            msgTxtBlock.Text = "Error while saving the trace data: " + ex.Message;
                    }
                }
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            msgTxtBlock.Text = "";

            int selectedTabIndex = TabControl.SelectedIndex;
            if (selectedTabIndex == 0)
            {
                if (messageData.Count > 0)
                {
                    DialogResult dr = System.Windows.Forms.MessageBox.Show("Existing Messages will get cleared.Do you want to continue?", "Confirmation", System.Windows.Forms.MessageBoxButtons.OKCancel);
                    if (dr == System.Windows.Forms.DialogResult.Cancel)
                        return;
                }
            }
            else
            {
                if (traceData.Count > 0)
                {
                    DialogResult dr = System.Windows.Forms.MessageBox.Show("Existing trace will get cleared.Do you want to continue?", "Confirmation", System.Windows.Forms.MessageBoxButtons.OKCancel);
                    if (dr == System.Windows.Forms.DialogResult.Cancel)
                        return;
                }
            }


            string path = "";
            ObservableCollection<Item> fileData = new ObservableCollection<Item>();
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                path = dlg.FileName;
            else
                return;


            XmlSerializer xs = new XmlSerializer(typeof(ObservableCollection<Item>));
            using (StreamReader reader = new StreamReader(path))
            {
                try
                {
                    fileData = (ObservableCollection<Item>)xs.Deserialize(reader);
                }
                catch(Exception ex)
                {
                    if (Utilities.GetAppsettingsValue("displayErrors") == "True")
                        msgTxtBlock.Text = "Error while opening the file: " + ex.Message;
                    return;
                }
            }

            if (selectedTabIndex == 0)
            {
                messageData.Clear();
                messageId = 0;
                InitializeBrowserControls();


                foreach (Item item in fileData)
                {

                    messageData.Add(item);

                    //TBD RequestTime and ResponseTime data is not getting serialized and it is getting lost. Need to investigate
                    //Workaround to set this values
                    XElement requestXml = XElement.Parse(messageData[messageId].RequestXml);
                    DateTime requestTime = (DateTime)requestXml.Attribute("Time");
                    messageData[messageId].RequestTime = requestTime.TimeOfDay;

                    XElement responseXml = XElement.Parse(messageData[messageId].ResponseXml);
                    if (responseXml != null)
                    {
                        DateTime responseTime = (DateTime)responseXml.Attribute("Time");
                        messageData[messageId].ResponseTime = responseTime.TimeOfDay;
                    }
                   
                    messageId++;
                }
            }
            else
            {
                traceData.Clear();
                traceId = 0;
                traceXml.NavigateToString(" ");

                foreach (Item item in fileData)
                {
                    traceData.Add(item);
                    traceId++;
                }
            }

        }

        private void Configure_Click(object sender, RoutedEventArgs e)
        {
            msgTxtBlock.Text = "";

            bool  result;
            TraceConfigure configure = new TraceConfigure();
            configure.Owner = System.Windows.Application.Current.MainWindow; ;
            configure.ShowInTaskbar = false;
            configure.ShowDialog();
            string tracingStatus = Utilities.GetAppsettingsValue("tracingEnabled"); 
            if (tracingStatus != "")
            {
                result = bool.TryParse(tracingStatus, out tracingEnabled);
            }
        }

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            int selectedTabIndex = TabControl.SelectedIndex;
            if (selectedTabIndex == 0)
            {
                // message Tab
                foreach (var item in messageData)
                {
                    item.IsHightlighted = false;
                }

                var test = from v in messageData
                           where (v.RequestXml.Contains(txtFind.Text) || (v.ResponseXml.Contains(txtFind.Text)))
                           select v;

                foreach (var item in test)
                {
                    item.IsHightlighted = true;
                }

            }
            else
            {
                // Trace Tab
                foreach (var item in traceData)
                {
                    item.IsHightlighted = false;
                }

                // TBD does not search in traceData?
                var test = from v in traceData
                           where (v.RequestXml.Contains(txtFind.Text))
                           select v;

                foreach (var item in test)
                {
                    item.IsHightlighted = true;
                }

            }

        }

        private void txtFind_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            txtFind.Text = "";
        }

        private void InitializeBrowserControls()
        {
            RequestHeaderXml.NavigateToString(" ");
            RequestEnvelopeXml.NavigateToString(" ");
            RequestRawXml.Text = "";

            ResponseHeaderXml.NavigateToString(" ");
            ResponseEnvelopeXml.NavigateToString(" ");
            ResponseRawXml.Text = "";
        }
    }
}
