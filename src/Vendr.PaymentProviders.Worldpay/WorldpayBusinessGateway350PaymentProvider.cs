using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vendr.Core.Models;
using Vendr.Core.Api;
using Vendr.Core.PaymentProviders;
using Vendr.PaymentProviders.Worldpay.Helpers;
using Vendr.Common.Logging;
using Vendr.Extensions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web;
using System.Collections.Specialized;

namespace Vendr.PaymentProviders.Worldpay
{
    [PaymentProvider("worldpay-bs350", "Worldpay Business Gateway 350", "Worldpay Business Gateway 350 payment provider", Icon = "icon-credit-card")]
    public class WorldpayBusinessGateway350PaymentProvider : PaymentProviderBase<WorldpayBusinessGateway350Settings>
    {
        private const string LiveBaseUrl = "https://secure.worldpay.com/wcc/purchase";
        private const string TestBaseUrl = "https://secure-test.worldpay.com/wcc/purchase";

        private readonly ILogger<WorldpayBusinessGateway350PaymentProvider> _logger;

        public override bool FinalizeAtContinueUrl => false;

        public WorldpayBusinessGateway350PaymentProvider(VendrContext vendr,
            ILogger<WorldpayBusinessGateway350PaymentProvider> logger) 
            : base(vendr)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public override string GetCancelUrl(PaymentProviderContext<WorldpayBusinessGateway350Settings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.CancelUrl.MustNotBeNull("ctx.Settings.CancelUrl");

            return ctx.Settings.CancelUrl;
        }

        public override string GetContinueUrl(PaymentProviderContext<WorldpayBusinessGateway350Settings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.ContinueUrl.MustNotBeNull("ctx.Settings.ContinueUrl");

            return ctx.Settings.ContinueUrl;
        }

        public override string GetErrorUrl(PaymentProviderContext<WorldpayBusinessGateway350Settings> ctx)
        {
            ctx.Settings.MustNotBeNull("ctx.Settings");
            ctx.Settings.ErrorUrl.MustNotBeNull("ctx.Settings.ErrorUrl");

            return ctx.Settings.ErrorUrl;
        }

        public override Task<PaymentFormResult> GenerateFormAsync(PaymentProviderContext<WorldpayBusinessGateway350Settings> ctx)
        {
            try
            {
                if (ctx.Settings.VerboseLogging)
                {
                    _logger.Info($"GenerateForm method called for cart {ctx.Order.OrderNumber}");
                }

                ctx.Settings.InstallId.MustNotBeNull("ctx.Settings.InstallId");

                var firstname = ctx.Order.CustomerInfo.FirstName;
                var surname = ctx.Order.CustomerInfo.LastName;

                if (!string.IsNullOrEmpty(ctx.Settings.BillingFirstNamePropertyAlias))
                {
                    firstname = ctx.Order.Properties[ctx.Settings.BillingFirstNamePropertyAlias];
                }

                if (!string.IsNullOrEmpty(ctx.Settings.BillingLastNamePropertyAlias))
                {
                    surname = ctx.Order.Properties[ctx.Settings.BillingLastNamePropertyAlias];
                }

                var address1 = ctx.Order.Properties[ctx.Settings.BillingAddressLine1PropertyAlias] ?? string.Empty;
                var city = ctx.Order.Properties[ctx.Settings.BillingAddressCityPropertyAlias] ?? string.Empty;
                var postcode = ctx.Order.Properties[ctx.Settings.BillingAddressZipCodePropertyAlias] ?? string.Empty;
                var billingCountry = Vendr.Services.CountryService.GetCountry(ctx.Order.PaymentInfo.CountryId.Value);
                var billingCountryCode = billingCountry.Code.ToUpperInvariant();
                var amount = ctx.Order.TransactionAmount.Value.Value.ToString("0.00", CultureInfo.InvariantCulture);
                var currency = Vendr.Services.CurrencyService.GetCurrency(ctx.Order.CurrencyId);
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
                    { "instId", ctx.Settings.InstallId },
                    { "testMode", ctx.Settings.TestMode ? "100" : "0" },
                    { "authMode", ctx.Settings.Capture ? "A" : "E" },
                    { "cartId", ctx.Order.OrderNumber },
                    { "amount", amount },
                    { "currency", currencyCode },
                    { "name", firstname + " " + surname },
                    { "email", ctx.Order.CustomerInfo.Email },
                    { "address1", address1 },
                    { "town", city },
                    { "postcode", postcode },
                    { "country", billingCountryCode },
                    { "MC_ctx.OrderRef", ctx.Order.GenerateOrderReference() },
                    { "MC_cancelurl", ctx.Urls.CancelUrl },
                    { "MC_returnurl", ctx.Urls.ContinueUrl },
                    { "MC_callbackurl", ctx.Urls.CallbackUrl }
                };

                if (!string.IsNullOrEmpty(ctx.Settings.Md5Secret))
                {
                    var orderSignature = Md5Helper.CreateMd5(ctx.Settings.Md5Secret + ":" + amount + ":" + currencyCode + ":" + ctx.Settings.InstallId + ":" + ctx.Order.OrderNumber);

                    orderDetails.Add("signature", orderSignature);

                    if (ctx.Settings.VerboseLogging)
                    {
                        _logger.Info($"Before Md5: " + ctx.Settings.Md5Secret + ":" + amount + ":" + currencyCode + ":" + ctx.Settings.InstallId + ":" + ctx.Order.OrderNumber);
                        _logger.Info($"Signature: " + orderSignature);
                    }
                }

                var url = ctx.Settings.TestMode ? TestBaseUrl : LiveBaseUrl;
                var form = new PaymentForm(url, PaymentFormMethod.Post)
                {
                    Inputs = orderDetails
                };

                if (ctx.Settings.VerboseLogging)
                {
                    _logger.Info($"Payment url {url}");
                    _logger.Info($"Form data {orderDetails.ToFriendlyString()}");
                }

                return Task.FromResult(new PaymentFormResult()
                {
                    Form = form
                });
            }
            catch (Exception e)
            {
                _logger.Error($"Exception thrown for cart {ctx.Order.OrderNumber} - with error {e.Message}");

                throw;
            }
        }

        public override async Task<OrderReference> GetOrderReferenceAsync(PaymentProviderContext<WorldpayBusinessGateway350Settings> ctx)
        {
            ctx.Request.MustNotBeNull("ctx.Request");
            ctx.Settings.MustNotBeNull("ctx.Settings");

            var queryData = HttpUtility.ParseQueryString(ctx.Request.RequestUri.Query);;
            var formData = await ctx.Request.Content.ReadAsFormDataAsync();

            ctx.AdditionalData.Add("queryData", queryData);
            ctx.AdditionalData.Add("formData", formData);

            if (ctx.Settings.VerboseLogging)
            {
                _logger.Info($"Worldpay data {formData.ToFriendlyString()}");
            }

            if (!string.IsNullOrEmpty(ctx.Settings.ResponsePassword))
            {
                // Validate password
                if (ctx.Settings.ResponsePassword != formData["callbackPW"])
                {
                    return null;
                }
            }

            if (OrderReference.TryParse(formData["MC_ctx.OrderRef"], out var orderReference))
                return orderReference;

            return await base.GetOrderReferenceAsync(ctx);
        }

        public override Task<CallbackResult> ProcessCallbackAsync(PaymentProviderContext<WorldpayBusinessGateway350Settings> ctx)
        {
            // The request stream is processed inside GetOrderReferenceAsync and the relevant data
            // is stored in the payment provider context to prevent needing to re-process it
            // so we just access it directly from the context assuming it exists.
            var queryData = ctx.AdditionalData["queryData"] as NameValueCollection;
            var formData = ctx.AdditionalData["formData"] as NameValueCollection;

            if (queryData["msgType"] == "authResult")
            {
                _logger.Info($"Payment call back for cart {ctx.Order.OrderNumber}");

                if (ctx.Settings.VerboseLogging)
                {
                    _logger.Info($"Worldpay data {formData.ToFriendlyString()}");
                }

                if (!string.IsNullOrEmpty(ctx.Settings.ResponsePassword))
                {
                    // validate password
                    if (ctx.Settings.ResponsePassword != formData["callbackPW"])
                    {
                        _logger.Info($"Payment call back for cart {ctx.Order.OrderNumber} response password incorrect");

                        return Task.FromResult(CallbackResult.Ok());
                    }
                }

                // if still here, password was not required or matched
                if (formData["transStatus"] == "Y")
                {
                    var totalAmount = decimal.Parse(formData["authAmount"], CultureInfo.InvariantCulture);
                    var transactionId = formData["transId"];
                    var paymentStatus = formData["authMode"] == "A" ? PaymentStatus.Authorized : PaymentStatus.Captured;

                    _logger.Info($"Payment call back for cart {ctx.Order.OrderNumber} payment authorised");

                    return Task.FromResult(CallbackResult.Ok(new TransactionInfo
                    {
                        AmountAuthorized = totalAmount,
                        TransactionFee = 0m,
                        TransactionId = transactionId,
                        PaymentStatus = paymentStatus
                    }));
                }
                else
                {
                    _logger.Info($"Payment call back for cart {ctx.Order.OrderNumber} payment not authorised or error");
                }
            }

            return Task.FromResult(CallbackResult.Ok());
        }
    }
}