using UserHelpers.Helpers;

namespace Test._App
{

    public class ECData : IECData
    {
        public string Name { get; set; }
        public string ErrorCode { get; set; }
        public string CustomerCode { get; set; }
        public string ErrorDescription { get; set; }

        public ECData()
        {
            Name = string.Empty;
            ErrorCode = string.Empty;
            CustomerCode = string.Empty;
            ErrorDescription = string.Empty;
        }

        public IECData Clone()
        {
            return new ECData()
            {
                Name = Name,
                ErrorCode = ErrorCode,
                CustomerCode = CustomerCode,
                ErrorDescription = ErrorDescription
            };
        }
    }
}
