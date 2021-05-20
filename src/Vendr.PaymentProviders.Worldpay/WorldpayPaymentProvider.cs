using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Vendr.Core;
using Vendr.Core.Logging;
using Vendr.Core.Models;
using Vendr.Core.Services;
using Vendr.Core.Web.Api;
using Vendr.Core.Web.PaymentProviders;
using Vendr.PaymentProviders.Worldpay.Helpers;

namespace Vendr.PaymentProviders.Worldpay
{
    [PaymentProvider("worldpay", "Worldpay", "Worldpay payment provider", Icon = "icon-credit-card")]
    public class WorldpayPaymentProvider : PaymentProviderBase<WorldpaySettings>
    {
        private readonly ILogger _logger;

        public WorldpayPaymentProvider(VendrContext vendr, ILogger logger) : base(vendr)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override bool FinalizeAtContinueUrl => false;

        public override PaymentFormResult GenerateForm(OrderReadOnly order, string continueUrl, string cancelUrl, string callbackUrl, WorldpaySettings settings)
        {
            try
            {
                if (settings.VerboseLogging)
                {
                    _logger.Info<WorldpayPaymentProvider>($"GenerateForm method called for cart {order.OrderNumber}");
                }

                var url = settings.Mode.ToLower() == "live" ? settings.LiveUrl : settings.TestUrl;
                var form = new PaymentForm(url, FormMethod.Post);

                settings.InstallId.MustNotBeNull("settings.InstallId");
                settings.TestModeNumber.MustNotBeNull("settings.TestModeNumber");
                settings.AuthMode.MustNotBeNull("settings.AuthMode");

                var firstname = order.CustomerInfo.FirstName;
                var surname = order.CustomerInfo.LastName;

                if (!string.IsNullOrEmpty(settings.OrderPropertyBillingFirstName))
                {
                    firstname = order.Properties[settings.OrderPropertyBillingFirstName];
                }

                if (!string.IsNullOrEmpty(settings.OrderPropertyBillingLastName))
                {
                    surname = order.Properties[settings.OrderPropertyBillingLastName];
                }

                var address1 = order.Properties[settings.OrderPropertyBillingAddress1] ?? string.Empty;
                var city = order.Properties[settings.OrderPropertyBillingCity] ?? string.Empty;
                var postcode = order.Properties[settings.OrderPropertyBillingPostcode] ?? string.Empty;
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
                    { "testMode", settings.TestModeNumber },
                    { "authMode", settings.AuthMode },
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
                        _logger.Info<WorldpayPaymentProvider>($"Before Md5: " + settings.Md5Secret + ":" + amount + ":" + currencyCode + ":" + settings.InstallId + ":" + order.OrderNumber);
                        _logger.Info<WorldpayPaymentProvider>($"Signature: " + orderSignature);
                    }
                }

                form.Inputs = orderDetails;

                if (settings.VerboseLogging)
                {
                    _logger.Info<WorldpayPaymentProvider>($"Payment url {url}");
                    _logger.Info<WorldpayPaymentProvider>($"Form data {orderDetails.ToFriendlyString()}");
                }

                return new PaymentFormResult()
                {
                    Form = form
                };
            }
            catch (Exception e)
            {
                _logger.Error<WorldpayPaymentProvider>($"Exception thrown for cart {order.OrderNumber} - with error {e.Message}");
                throw;
            }
        }

        public override string GetCancelUrl(OrderReadOnly order, WorldpaySettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.CancelUrl.MustNotBeNull("settings.CancelUrl");

            return settings.CancelUrl;
        }

        public override string GetErrorUrl(OrderReadOnly order, WorldpaySettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ErrorUrl.MustNotBeNull("settings.ErrorUrl");

            return settings.ErrorUrl;
        }

        public override string GetContinueUrl(OrderReadOnly order, WorldpaySettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ContinueUrl.MustNotBeNull("settings.ContinueUrl");

            return settings.ContinueUrl;
        }

        public override CallbackResult ProcessCallback(OrderReadOnly order, HttpRequestBase request, WorldpaySettings settings)
        {
            if (request.QueryString["msgType"] == "authResult")
            {
                _logger.Info<WorldpayPaymentProvider>($"Payment call back for cart {order.OrderNumber}");

                if (settings.VerboseLogging)
                {
                    _logger.Info<WorldpayPaymentProvider>($"Worldpay data {request.Form.ToFriendlyString()}");
                }

                var response = new CallbackResult();

                if (!string.IsNullOrEmpty(settings.ResponsePassword))
                {
                    // validate password
                    if (settings.ResponsePassword != request.Form["callbackPW"])
                    {
                        response.TransactionInfo = new TransactionInfo
                        {
                            AmountAuthorized = 0,
                            TransactionFee = 0m,
                            TransactionId = Guid.NewGuid().ToString("N"),
                            PaymentStatus = PaymentStatus.Error
                        };

                        _logger.Info<WorldpayPaymentProvider>($"Payment call back for cart {order.OrderNumber} response password incorrect");
                        return response;
                    }
                }

                // if still here, password was not required or matched
                if (request.Form["transStatus"] == "Y")
                {
                    var totalAmount = decimal.Parse(request.Form["authAmount"], CultureInfo.InvariantCulture);
                    var transaction = request.Form["transId"];
                    var paymentState = request.Form["authMode"] == "A" ? PaymentStatus.Authorized : PaymentStatus.Captured;

                    _logger.Info<WorldpayPaymentProvider>($"Payment call back for cart {order.OrderNumber} payment authorised");

                    response.TransactionInfo = new TransactionInfo
                    {
                        AmountAuthorized = totalAmount,
                        TransactionFee = 0m,
                        TransactionId = transaction,
                        PaymentStatus = paymentState
                    };
                }
                else
                {
                    _logger.Info<WorldpayPaymentProvider>($"Payment call back for cart {order.OrderNumber} payment not authorised or error");

                    response.TransactionInfo = new TransactionInfo
                    {
                        AmountAuthorized = 0,
                        TransactionFee = 0m,
                        TransactionId = Guid.NewGuid().ToString("N"),
                        PaymentStatus = PaymentStatus.Error
                    };
                }

                return response;
            }

            return CallbackResult.Ok();
        }
    }
}