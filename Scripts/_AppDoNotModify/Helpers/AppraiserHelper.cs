using System.Collections.Generic;

namespace Test._ScriptHelpers
{

    //    状态     Pass    Fail
    //当前一SN为空	+1	   不变
    //与前一SN相同	+1	   不变
    //与前一SN不同  不变    不变

    //+1发生成上传MES之后，上传MES时并未+1		
    //只对ONLINE时有效
    //如果上次测试没读到SN，也算上次SN为空

    public class AppraiserHelper
    {
        string _lastSerialNumber = string.Empty;
        List<string> _names = new List<string>() { "A", "B", "C" };
        int _appraiser = 0;

        public AppraiserHelper()
        {
            _appraiser = 0;
            _lastSerialNumber = string.Empty;
        }

        public void Next(string sn)
        {
            if (string.IsNullOrEmpty(_lastSerialNumber) || _lastSerialNumber.Equals(sn))
            {
                _appraiser = _appraiser + 1;
                _appraiser = _appraiser % 3;
            }
            _lastSerialNumber = sn;
        }

        public string Get()
        {
            return _names[_appraiser];
        }
    }
}
