using Vendr.Core.Web.PaymentProviders;

namespace Vendr.PaymentProviders.Worldpay
{
    public class WorldpaySettings
    {
        [PaymentProviderSetting(Name = "Continue Url", Description = "The Continue URL", SortOrder = 1000)]
        public string ContinueUrl { get; set; }

        [PaymentProviderSetting(Name = "Cancel Url", Description = "The Cancel URL", SortOrder = 2000)]
        public string CancelUrl { get; set; }

        [PaymentProviderSetting(Name = "Error Url", Description = "The Error URL", SortOrder = 3000)]
        public string ErrorUrl { get; set; }

        [PaymentProviderSetting(Name = "Billing Property First Name", Description = "The order property alias containing the first name of the customer", SortOrder = 4000)]
        public string BillingFirstNamePropertyAlias { get; set; }

        [PaymentProviderSetting(Name = "Billing Property Last Name", Description = "The order property alias containing the last name of the customer", SortOrder = 5000)]
        public string BillingLastNamePropertyAlias { get; set; }

        [PaymentProviderSetting(Name = "Billing Address (Line 1) Property Alias", Description = "The order property alias containing line 1 of the billing address", SortOrder = 6000)]
        public string BillingAddressLine1PropertyAlias { get; set; }

        [PaymentProviderSetting(Name = "Billing Address City Property Alias", Description = "The order property alias containing the city of the billing address", SortOrder = 7000)]
        public string BillingAddressCityPropertyAlias { get; set; }

        [PaymentProviderSetting(Name = "Billing Address ZipCode Property Alias", Description = "The order property alias containing the zip code of the billing address", SortOrder = 8000)]
        public string BillingAddressZipCodePropertyAlias { get; set; }

        [PaymentProviderSetting(Name = "Install ID", Description = "The installation ID", SortOrder = 9000)]
        public string InstallId { get; set; }

        [PaymentProviderSetting(Name = "Test URL", Description = "Default: https://secure-test.worldpay.com/wcc/purchase", SortOrder = 10000)]
        public string TestUrl { get; set; }

        [PaymentProviderSetting(Name = "Live URL", Description = "Default: https://secure.worldpay.com/wcc/purchase", SortOrder = 11000)]
        public string LiveUrl { get; set; }

        [PaymentProviderSetting(Name = "Auth Mode", Description = "A for a full authorisation, or E for a pre-authorisation", SortOrder = 12000)]
        public WorldpayAuthMode AuthMode { get; set; }

        [PaymentProviderSetting(Name = "MD5 Secret", Description = "If enabled enter secret, fields are amount, currency, instId and cartId parameters", SortOrder = 13000)]
        public string Md5Secret { get; set; }

        [PaymentProviderSetting(Name = "Response Password", Description = "If enabled enter response password", SortOrder = 14000)]
        public string ResponsePassword { get; set; }

        [PaymentProviderSetting(Name = "LiveMode", Description = "Enable LIVE mode", SortOrder = 15000)]
        public bool LiveMode { get; set; }

        [PaymentProviderSetting(Name = "Verbose Logging", Description = "Enable verbose logging", IsAdvanced = true, SortOrder = 16000)]
        public bool VerboseLogging { get; set; }
    }
}