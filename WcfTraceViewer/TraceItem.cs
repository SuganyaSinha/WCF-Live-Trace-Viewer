using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace WcfLiveTraceViewer
{
    public class Item : INotifyPropertyChanged
    {
        private string requestXml;
        private string responseXml;
        private int id;
        private string activityId;
        private TimeSpan requestTime;
        private TimeSpan responseTime;
        private string action;
        private string sentTo;
        private string processName;
        private long duration = 3;
        private bool isHightlighted = false;
        private string source;
        private bool hasError = false;
        private string description;

        public Item()
        {

        }
        public Item(string requestXml, int id, string activityId, TimeSpan requestTime, string action, string sentTo, string processName,string source,bool hasError)
        {
            RequestXml = requestXml;
            Id = id;
            ActivityId = activityId;
            Action = action;
            RequestTime = requestTime;
            SentTo = sentTo;
            ProcessName = processName;
            Source = source;
            HasError = hasError;
        }

        public Item(string requestXml, int id, string activityId, string processName, bool hasError, string descripton)
        {
            RequestXml = requestXml;
            Id = id;
            ActivityId = activityId;
            ProcessName = processName;
            HasError = hasError;
            Description = descripton;
        }

        public string RequestXml
        {
            get { return requestXml; }
            set { requestXml = value; }
        }

        public string ResponseXml
        {
            get { return responseXml; }
            set { responseXml = value; }
        }

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public string ActivityId
        {
            get { return activityId; }
            set { activityId = value; }
        }

        public TimeSpan RequestTime
        {
            get { return requestTime; }
            set { requestTime = value; }
        }

        public TimeSpan ResponseTime
        {
            get { return responseTime; }
            set
            {
                responseTime = value;
                if (responseTime.ToString() != "")
                {
                    OnPropertyChanged(new PropertyChangedEventArgs("ResponseTime"));
                    TimeSpan difference = responseTime - requestTime;
                    Duration = difference.Milliseconds;

                }

            }
        }

        public long Duration
        {
            get { return duration; }
            set
            {
                duration = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Duration"));
            }
        }

        public bool IsHightlighted
        {
            get { return isHightlighted; }
            set
            {
                isHightlighted = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsHightlighted"));
            }
        }
        public string Action
        {
            get { return action; }
            set { action = value; }
        }

        public string SentTo
        {
            get { return sentTo; }
            set { sentTo = value; }
        }

        public string ProcessName
        {
            get { return processName; }
            set { processName = value; }
        }

        public string Source
        {
            get { return source; }
            set { source = value; }
        }

        public bool HasError
        {
            get { return hasError; }
            set
            {
                hasError = value;
                OnPropertyChanged(new PropertyChangedEventArgs("HasError"));
            }
        }

        public string Description
        {
            get { return description; }
            set
            {
                description = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }

    }
}
