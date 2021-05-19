using Vendr.Core.Web.PaymentProviders;

namespace Vendr.PaymentProviders.Worldpay
{
    public class WorldpaySettings
    {
        [PaymentProviderSetting(Name = "Cancel Url", Description = "The Cancel URL")]
        public string CancelUrl { get; set; }

        [PaymentProviderSetting(Name = "Error Url", Description = "The Error URL")]
        public string ErrorUrl { get; set; }

        [PaymentProviderSetting(Name = "Continue Url", Description = "The Continue URL")]
        public string ContinueUrl { get; set; }

        [PaymentProviderSetting(Name = "Install ID", Description = "The installation ID")]
        public string InstallId { get; set; }

        [PaymentProviderSetting(Name = "Mode", Description = "TEST or LIVE")]
        public string Mode { get; set; }

        [PaymentProviderSetting(Name = "Test Mode Number", Description = "100 for TEST, 0 for LIVE")]
        public string TestModeNumber { get; set; }

        [PaymentProviderSetting(Name = "Test URL", Description = "Default: https://secure-test.worldpay.com/wcc/purchase")]
        public string TestUrl { get; set; }

        [PaymentProviderSetting(Name = "Live URL", Description = "Default: https://secure.worldpay.com/wcc/purchase")]
        public string LiveUrl { get; set; }
        
        [PaymentProviderSetting(Name = "Auth Mode", Description = "A for a full authorisation, or E for a pre-authorisation")]
        public string AuthMode { get; set; }

        [PaymentProviderSetting(Name = "MD5 Secret", Description = "If enabled enter secret, fields are amount, currency, instId and cartId parameters")]
        public string Md5Secret { get; set; }

        [PaymentProviderSetting(Name = "Response Password", Description = "If enabled enter response password")]
        public string ResponsePassword { get; set; }

        [PaymentProviderSetting(Name = "Verbose Logging", Description = "Enable verbose logging")]
        public bool VerboseLogging { get; set; }

        [PaymentProviderSetting(Name = "Order property alias: Billing Last Name", Description = "Order property alias containing the billing last name")]
        public string OrderPropertyBillingLastName { get; set; }

        [PaymentProviderSetting(Name = "Order property alias: Billing First Name", Description = "Order property alias containing the billing first name")]
        public string OrderPropertyBillingFirstName { get; set; }

        [PaymentProviderSetting(Name = "Order property alias: Billing Address 1", Description = "Order property alias containing the billing address 1")]
        public string OrderPropertyBillingAddress1 { get; set; }
        
        [PaymentProviderSetting(Name = "Order property alias: Billing City", Description = "Order property alias containing the billing city")]
        public string OrderPropertyBillingCity { get; set; }

        [PaymentProviderSetting(Name = "Order property alias: Billing Postcode", Description = "Order property alias containing the billing postcode")]
        public string OrderPropertyBillingPostcode { get; set; }
    }
}