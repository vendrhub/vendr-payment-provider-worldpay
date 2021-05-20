using System.ComponentModel;

namespace Vendr.PaymentProviders.Worldpay
{
    public enum WorldpayAuthMode
    {
        [Description("Full Authorization")]
        A,
        [Description("Pre-Authorization")]
        E
    }
}