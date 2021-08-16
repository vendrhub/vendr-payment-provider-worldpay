using Vendr.Core.Web.PaymentProviders;

namespace Vendr.PaymentProviders.Worldpay
{
    public class WorldpayAccessOneTimeSettings
    {
        [PaymentProviderSetting(Name = "Continue URL",
            Description = "The URL to continue to after this provider has done processing. eg: /continue/",
            SortOrder = 100)]
        public string ContinueUrl { get; set; }

        [PaymentProviderSetting(Name = "Cancel URL",
            Description = "The URL to return to if the payment attempt is canceled. eg: /cancel/",
            SortOrder = 200)]
        public string CancelUrl { get; set; }

        [PaymentProviderSetting(Name = "Error URL",
            Description = "The URL to return to if the payment attempt errors. eg: /error/",
            SortOrder = 300)]
        public string ErrorUrl { get; set; }

        [PaymentProviderSetting(Name = "Test Client Key",
            Description = "Your test Worldpay client key",
            SortOrder = 400)]
        public string TestClientKey { get; set; }

        [PaymentProviderSetting(Name = "Test Service Key",
            Description = "Your test Worldpay service key",
            SortOrder = 500)]
        public string TestServiceKey { get; set; }

        [PaymentProviderSetting(Name = "Live Client Key",
            Description = "Your live Worldpay client key",
            SortOrder = 600)]
        public string LiveClientKey { get; set; }

        [PaymentProviderSetting(Name = "Live Service Key",
            Description = "Your live Worldpay service key",
            SortOrder = 700)]
        public string LiveServiceKey { get; set; }

        [PaymentProviderSetting(Name = "Test Mode",
            Description = "Set whether to process payments in test mode.",
            SortOrder = 10000)]
        public bool TestMode { get; set; }
    }
}
