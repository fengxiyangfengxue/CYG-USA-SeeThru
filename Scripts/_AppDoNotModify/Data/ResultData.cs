using System;
using System.ComponentModel;
using UserHelpers.Helpers;

namespace Test._App
{
    public class ResultData : IResultData
    {
        string _isLatest = null;
        DateTime? _testEndTime = null;
        DateTime? _teststartTime = null;

        string _testName = string.Empty;
        string _value = string.Empty;
        string _upperLimit = string.Empty;
        string _lowerLimit = string.Empty;
        string _unit = string.Empty;
        string _message = string.Empty;
        string _ecName = string.Empty;
        string _itemTitle = string.Empty;
        DateTime _dataCreateTime;

        int? _retryIndex = null;
        int? _totalRetryIndex = null;
        string _ticks = "0";


        public string TestName
        {
            get { return _testName; }
            set
            {
                _testName = value;
                NotifyPropertyChanged("TestName");
            }
        }

        public string Value
        {
            get { return _value; }
            set
            {
                _value = value;
                NotifyPropertyChanged("Value");
            }
        }


        public string UpperLimit
        {
            get { return _upperLimit; }
            set
            {
                _upperLimit = value;
                NotifyPropertyChanged("UpperLimit");
            }
        }

        public string LowerLimit
        {
            get { return _lowerLimit; }
            set
            {
                _lowerLimit = value;
                NotifyPropertyChanged("LowerLimit");
            }
        }

        public string Unit
        {
            get { return _unit; }
            set
            {
                _unit = value;
                NotifyPropertyChanged("Unit");
            }
        }

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                NotifyPropertyChanged("Message");
            }
        }

        public string ECName
        {
            get { return _ecName; }
            set
            {
                _ecName = value;
                NotifyPropertyChanged("ECName");
            }
        }

        public string ItemTitle
        {
            get { return _itemTitle; }
            set
            {
                _itemTitle = value;
                NotifyPropertyChanged("ItemTitle");
            }
        }

        public DateTime DataCreateTime
        {
            get { return _dataCreateTime; }
            set
            {
                _dataCreateTime = value;
                NotifyPropertyChanged("DataCreateTime");
            }
        }

        public int? RetryIndex
        {
            get { return _retryIndex; }
            set
            {
                _retryIndex = value;
                NotifyPropertyChanged("RetryIndex");
            }
        }
        public int? TotalRetryIndex
        {
            get { return _totalRetryIndex; }
            set
            {
                _totalRetryIndex = value;
                NotifyPropertyChanged("TotalRetryIndex");
            }
        }

        public string IsLatest
        {
            get { return _isLatest; }
            set
            {
                _isLatest = value;
                NotifyPropertyChanged("IsLatest");
            }
        }

        public DateTime? TestStartTime
        {
            get { return _teststartTime; }
            set
            {
                _teststartTime = value;
                NotifyPropertyChanged("TestStartTime");

                if (TestStartTime != null && TestEndTime != null) 
                    Ticks= ((DateTime)TestEndTime - (DateTime)TestStartTime).TotalSeconds.ToString("F3");
                 
            }
        }

        public DateTime? TestEndTime
        {
            get { return _testEndTime; }
            set
            {
                _testEndTime = value;
                NotifyPropertyChanged("TestEndTime");
                if (TestStartTime != null && TestEndTime != null)
                    Ticks = ((DateTime)TestEndTime - (DateTime)TestStartTime).TotalSeconds.ToString("F3");
            }
        }

        public string Status
        {
            get { return string.IsNullOrEmpty(ECName) ? "1" : "0"; }
        }
        public string Result
        {
            get { return string.IsNullOrEmpty(ECName) ? "PASS" : "FAIL"; }
        }

        public IECData Error { get; set; }
         
        public Guid Guid { get; set; } = Guid.NewGuid();


        public string Ticks
        {
            get { return _ticks; }
            set
            {
                _ticks = value;
                NotifyPropertyChanged("Ticks");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged(String name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        void setData(string item, string value, string unit, string upperLimit, string lowerLimit, string errorMessage, string ecName)
        {
            TestName = item;
            Value = value;
            Unit = unit;
            UpperLimit = upperLimit;
            LowerLimit = lowerLimit;
            Message = errorMessage;
            ECName = ecName == null ? string.Empty : ecName;
            RetryIndex = null;
            TotalRetryIndex = null;
            TestStartTime = null;
            TestEndTime = null;
            ItemTitle = null;
            IsLatest = "0";
        }

        public ResultData()
        {
            setData(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }
        public ResultData(string item)
        {
            setData(item, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }
        public ResultData(string item, string ecName)
        {
            setData(item, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, ecName);
        }
        
        public ResultData(string item, string ecName, string value)
        {
            setData(item, value, string.Empty, string.Empty, string.Empty, string.Empty, ecName);
        }

        public ResultData(string item, string ecName, string value, string unit)
        {
            setData(item, value, unit, string.Empty, string.Empty, string.Empty, ecName);
        }

        public ResultData(string item, string ecName, string value, string unit, string upperLimit, string lowerLimit)
        {
            setData(item, value, unit, upperLimit, lowerLimit, string.Empty, ecName);
        }

        public ResultData(string item, string ecName, string value, string upperLimit, string lowerLimit)
        {
            setData(item, value, string.Empty, upperLimit, lowerLimit, string.Empty, ecName);
        }

        public ResultData(string item, string ecName, string value, string unit, string upperLimit, string lowerLimit, string errorMessage)
        {
            setData(item, value, unit, upperLimit, lowerLimit, errorMessage, ecName);
        }

        public void SetData(string item, string ecName)
        {
            setData(item, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, ecName);
        }

        public void SetData(string item, string ecName, string value)
        {
            setData(item, value, string.Empty, string.Empty, string.Empty, string.Empty, ecName);
        }

        public void SetData(string item, string ecName, string value, string upperLimit, string lowerLimit)
        {
            setData(item, value, string.Empty, upperLimit, lowerLimit, string.Empty, ecName);
        }

        public void SetData(string item, string ecName, string value, string unit, string upperLimit, string lowerLimit)
        {
            setData(item, value, unit, upperLimit, lowerLimit, string.Empty, ecName);
        }

        public void SetData(string item, string ecName, string value, string unit, string upperLimit, string lowerLimit, string errorMessage)
        {
            setData(item, value, unit, upperLimit, lowerLimit, errorMessage, ecName);
        }

        public void SetDatas(string item, string ecName, string value, string errorMessage = "")
        {
            TestName = item;
            Value = value;
            Message = errorMessage;
            ECName = ecName == null ? string.Empty : ecName;
            RetryIndex = null;
            TotalRetryIndex = null;
            TestStartTime = null;
            TestEndTime = null;
            ItemTitle = null;
            IsLatest = "0";
        }




        public string FunctionTicks { get; set; }

        public string ToLog()
        {
            string log = TestName + "," +
                        (string.IsNullOrEmpty(ECName) ? "1" : "0") + "," +
                        Value + "," +
                        LowerLimit + "," +
                        UpperLimit + "," +
                        Unit + "," +
                        (string.IsNullOrEmpty(ECName) ? "PASS" : "FAIL") + "," +
                        Message + "," +
                        (ItemTitle == null ? "" : ItemTitle) + "," +
                        (RetryIndex == null ? "" : ((int)RetryIndex).ToString()) + "," +
                        (TotalRetryIndex == null ? "" : ((int)TotalRetryIndex).ToString()) + "," +
                        IsLatest + "," +
                        (TestStartTime == null ? "" : ((DateTime)TestStartTime).ToString("MM-dd-yyyy HH:mm:ss.fff")) + "," +
                        (TestEndTime == null ? "" : ((DateTime)TestEndTime).ToString("MM-dd-yyyy HH:mm:ss.fff")) + "," +
                        Ticks;

            if (Error != null)
                log = log + "," +
                    (string.IsNullOrEmpty(ECName) ? "" : ECName) + "," +
                    Error.ErrorCode + "," +
                    Error.CustomerCode + "," +
                    Error.ErrorDescription;
            else
                log = log + ",,,,";

            return log;
        }


        public string ToSFISData()
        {
            string log = TestName + "," + (string.IsNullOrEmpty(ECName) ? "1" : "0") + "," + Value + "," + LowerLimit + "," + UpperLimit + "," + Unit;
            //if (string.IsNullOrEmpty(UpperLimit) == false || string.IsNullOrEmpty(LowerLimit) == false)
            //{
            //if (string.IsNullOrEmpty(UpperLimit))
            //    log = log + ",," + LowerLimit;
            //else if (string.IsNullOrEmpty(LowerLimit))
            //    log = log + "," + UpperLimit;
            //else
            //    log = log + "," + UpperLimit + "," + LowerLimit;
            //}
            return log;
        }

    }
}
