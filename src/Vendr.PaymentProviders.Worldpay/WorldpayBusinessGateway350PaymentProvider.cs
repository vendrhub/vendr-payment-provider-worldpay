using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Vendr.Core;
using Vendr.Core.Logging;
using Vendr.Core.Models;
using Vendr.Core.Web.Api;
using Vendr.Core.Web.PaymentProviders;
using Vendr.PaymentProviders.Worldpay.Helpers;

namespace Vendr.PaymentProviders.Worldpay
{
    [PaymentProvider("worldpay-bs350", "Worldpay Business Gateway 350", "Worldpay Business Gateway 350 payment provider", Icon = "icon-credit-card")]
    public class WorldpayBusinessGateway350PaymentProvider : PaymentProviderBase<WorldpayBusinessGateway350Settings>
    {
        private const string LiveBaseUrl = "https://secure.worldpay.com/wcc/purchase";
        private const string TestBaseUrl = "https://secure-test.worldpay.com/wcc/purchase";

        private readonly ILogger _logger;

        public WorldpayBusinessGateway350PaymentProvider(VendrContext vendr, ILogger logger) : base(vendr)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override bool FinalizeAtContinueUrl => false;

        public override PaymentFormResult GenerateForm(OrderReadOnly order, string continueUrl, string cancelUrl, string callbackUrl, WorldpayBusinessGateway350Settings settings)
        {
            try
            {
                if (settings.VerboseLogging)
                {
                    _logger.Info<WorldpayBusinessGateway350PaymentProvider>($"GenerateForm method called for cart {order.OrderNumber}");
                }

                settings.InstallId.MustNotBeNull("settings.InstallId");

                var firstname = order.CustomerInfo.FirstName;
                var surname = order.CustomerInfo.LastName;

                if (!string.IsNullOrEmpty(settings.BillingFirstNamePropertyAlias))
                {
                    firstname = order.Properties[settings.BillingFirstNamePropertyAlias];
                }

                if (!string.IsNullOrEmpty(settings.BillingLastNamePropertyAlias))
                {
                    surname = order.Properties[settings.BillingLastNamePropertyAlias];
                }

                var address1 = order.Properties[settings.BillingAddressLine1PropertyAlias] ?? string.Empty;
                var city = order.Properties[settings.BillingAddressCityPropertyAlias] ?? string.Empty;
                var postcode = order.Properties[settings.BillingAddressZipCodePropertyAlias] ?? string.Empty;
                var billingCountry = Vendr.Services.CountryService.GetCountry(order.PaymentInfo.CountryId.Value);
                var billingCountryCode = billingCountry.Code.ToUpperInvariant();
                var amount = order.TransactionAmount.Value.Value.ToString("0.00", CultureInfo.InvariantCulture);
                var currency = Vendr.Services.CurrencyService.GetCurrency(order.CurrencyId);
                var currencyCode = currency.Code.ToUpperInvariant();

                // Ensure billing country has valid ISO 3166 code
                var iso3166Countries = Vendr.Services.CountryService.GetIso3166CountryRegions();
                if (iso3166Countries.All(x => x.Code != billingCountryCode))
                {
                    throw new Exception("Country must be a valid ISO 3166 billing country code: " + billingCountry.Name);
                }

                // Ensure currency has valid ISO 4217 code
                if (!Iso4217.CurrencyCodes.ContainsKey(currencyCode))
                {
                    throw new Exception("Currency must be a valid ISO 4217 currency code: " + currency.Name);
                }

                var orderDetails = new Dictionary<string, string>
                {
                    { "instId", settings.InstallId },
                    { "testMode", settings.TestMode ? "100" : "0" },
                    { "authMode", settings.Capture ? "A" : "E" },
                    { "cartId", order.OrderNumber },
                    { "amount", amount },
                    { "currency", currencyCode },
                    { "MC_cancelurl", cancelUrl },
                    { "MC_returnurl", continueUrl },
                    { "MC_callbackurl", callbackUrl },
                    { "name", firstname + " " + surname },
                    { "email", order.CustomerInfo.Email },
                    { "address1", address1 },
                    { "town", city },
                    { "postcode", postcode },
                    { "country", billingCountryCode }
                };

                if (!string.IsNullOrEmpty(settings.Md5Secret))
                {
                    var orderSignature = Md5Helper.CreateMd5(settings.Md5Secret + ":" + amount + ":" + currencyCode + ":" + settings.InstallId + ":" + order.OrderNumber);

                    orderDetails.Add("signature", orderSignature);

                    if (settings.VerboseLogging)
                    {
                        _logger.Info<WorldpayBusinessGateway350PaymentProvider>($"Before Md5: " + settings.Md5Secret + ":" + amount + ":" + currencyCode + ":" + settings.InstallId + ":" + order.OrderNumber);
                        _logger.Info<WorldpayBusinessGateway350PaymentProvider>($"Signature: " + orderSignature);
                    }
                }

                var url = settings.TestMode ? TestBaseUrl : LiveBaseUrl;
                var form = new PaymentForm(url, FormMethod.Post)
                {
                    Inputs = orderDetails
                };

                if (settings.VerboseLogging)
                {
                    _logger.Info<WorldpayBusinessGateway350PaymentProvider>($"Payment url {url}");
                    _logger.Info<WorldpayBusinessGateway350PaymentProvider>($"Form data {orderDetails.ToFriendlyString()}");
                }

                return new PaymentFormResult()
                {
                    Form = form
                };
            }
            catch (Exception e)
            {
                _logger.Error<WorldpayBusinessGateway350PaymentProvider>($"Exception thrown for cart {order.OrderNumber} - with error {e.Message}");
                throw;
            }
        }

        public override string GetCancelUrl(OrderReadOnly order, WorldpayBusinessGateway350Settings settings)
        {
            settings.MustNotBeNull("settings");
            settings.CancelUrl.MustNotBeNull("settings.CancelUrl");

            return settings.CancelUrl;
        }

        public override string GetErrorUrl(OrderReadOnly order, WorldpayBusinessGateway350Settings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ErrorUrl.MustNotBeNull("settings.ErrorUrl");

            return settings.ErrorUrl;
        }

        public override string GetContinueUrl(OrderReadOnly order, WorldpayBusinessGateway350Settings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ContinueUrl.MustNotBeNull("settings.ContinueUrl");

            return settings.ContinueUrl;
        }

        public override CallbackResult ProcessCallback(OrderReadOnly order, HttpRequestBase request, WorldpayBusinessGateway350Settings settings)
        {
            if (request.QueryString["msgType"] == "authResult")
            {
                _logger.Info<WorldpayBusinessGateway350PaymentProvider>($"Payment call back for cart {order.OrderNumber}");

                if (settings.VerboseLogging)
                {
                    _logger.Info<WorldpayBusinessGateway350PaymentProvider>($"Worldpay data {request.Form.ToFriendlyString()}");
                }

                if (!string.IsNullOrEmpty(settings.ResponsePassword))
                {
                    // validate password
                    if (settings.ResponsePassword != request.Form["callbackPW"])
                    {
                        _logger.Info<WorldpayBusinessGateway350PaymentProvider>($"Payment call back for cart {order.OrderNumber} response password incorrect");

                        return CallbackResult.Ok();
                    }
                }

                // if still here, password was not required or matched
                if (request.Form["transStatus"] == "Y")
                {
                    var totalAmount = decimal.Parse(request.Form["authAmount"], CultureInfo.InvariantCulture);
                    var transactionId = request.Form["transId"];
                    var paymentStatus = request.Form["authMode"] == "A" ? PaymentStatus.Authorized : PaymentStatus.Captured;

                    _logger.Info<WorldpayBusinessGateway350PaymentProvider>($"Payment call back for cart {order.OrderNumber} payment authorised");

                    return CallbackResult.Ok(new TransactionInfo
                    {
                        AmountAuthorized = totalAmount,
                        TransactionFee = 0m,
                        TransactionId = transactionId,
                        PaymentStatus = paymentStatus
                    });
                }
                else
                {
                    _logger.Info<WorldpayBusinessGateway350PaymentProvider>($"Payment call back for cart {order.OrderNumber} payment not authorised or error");
                }
            }

            return CallbackResult.Ok();
        }
    }
}