
namespace Test._Definitions
{

    public class ItemLimit
    {
        public ItemLimit()
        {
            IsEnabled = true;
            UCL = null;
            LCL = null;
            LCLClosedInterval = true;
            UCLClosedInterval = true;
            Unit = string.Empty;
            CheckString = string.Empty;
            Message = string.Empty;
        }

        public bool IsEnabled { get; set; }
        public bool LCLClosedInterval { get; set; }
        public bool UCLClosedInterval { get; set; }
        public double? UCL { get; set; }
        public double? LCL { get; set; }

        public string Unit { get; set; }

        public string CheckString { get; set; }
        public string Message { get; set; }

        public string ToLog()
        {
            return "IsEnabled = " + IsEnabled + ", " +
                "LCL = " + (LCL == null ? "" : LCL.ToString()) + ", " +
                "UCL = " + (UCL == null ? "" : UCL.ToString()) + ", " +
                "[LCL] = " + LCLClosedInterval.ToString() + ", " +
                "[UCL] = " + UCLClosedInterval.ToString() + ", " +
                "Unit = " + Unit + ", " +
                "CheckString = " + CheckString + ", " +
                "Message = " + Message;
        }
    }

}
