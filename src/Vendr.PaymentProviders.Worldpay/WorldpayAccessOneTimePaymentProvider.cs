using System;
using System.Web;
using System.Web.Mvc;
using Vendr.Core;
using Vendr.Core.Models;
using Vendr.Core.Web.Api;
using Vendr.Core.Web.PaymentProviders;

namespace Vendr.PaymentProviders.Worldpay
{
    [PaymentProvider("worldpay-access-onetime", "Worldpay Access (One Time)", "Worldpay Access payment provider for one time payments")]
    public class WorldpayAccessOneTimePaymentProvider : PaymentProviderBase<WorldpayAccessOneTimeSettings>
    {
        public WorldpayAccessOneTimePaymentProvider(VendrContext vendr)
            : base(vendr)
        { }

        public override bool FinalizeAtContinueUrl => true;

        public override PaymentFormResult GenerateForm(OrderReadOnly order, string continueUrl, string cancelUrl, string callbackUrl, WorldpayAccessOneTimeSettings settings)
        {
            var clientKey = settings.TestMode ? settings.TestClientKey : settings.LiveClientKey;

            return new PaymentFormResult()
            {
                Form = new PaymentForm(continueUrl, FormMethod.Post)
                    .WithAttribute("id", "worldpayPaymentForm")
                    .WithAttribute("onsubmit", "return Worldpay.submitTemplateForm()")
                    .WithInput("callbackUrl", callbackUrl)
                    .WithJsFile("https://cdn.worldpay.com/v1/worldpay.js")
                    .WithJs(@"
                        window.onload = function() {

                            // Ensure a payment section element
                            var paymentSectionEl = document.getElementById('worldpayPaymentSection');
                            if (!paymentSectionEl) {
                                var _el = document.createElement('div');
                                _el.id = 'worldpayPaymentSection';
                                document.getElementById('worldpayPaymentForm').appendChild('worldpayPaymentSection');
                            }

                            // Init worldpay
                            Worldpay.useTemplateForm({
                                'clientKey':'" + clientKey + @"',
                                'form':'worldpayPaymentForm',
                                'paymentSection':'worldpayPaymentSection',
                                'display':'modal',
                                'reusable':true,
                                'callback': function(obj) {
                                    if (obj && obj.token) {
                                        var _el = document.createElement('input');
                                        _el.value = obj.token;
                                        _el.type = 'hidden';
                                        _el.name = 'token';
                                        document.getElementById('worldpayPaymentForm').appendChild(_el);
                                        document.getElementById('worldpayPaymentForm').submit();
                                    }
                                }
                            });

                        }

                        window.handleWorldpayCheckout = function (e) {
                            e.preventDefault();
                            Worldpay.submitTemplateForm();
                            return false;
                        };
                    ")
            };
        }

        public override string GetCancelUrl(OrderReadOnly order, WorldpayAccessOneTimeSettings settings)
        {
            return string.Empty;
        }

        public override string GetErrorUrl(OrderReadOnly order, WorldpayAccessOneTimeSettings settings)
        {
            return string.Empty;
        }

        public override string GetContinueUrl(OrderReadOnly order, WorldpayAccessOneTimeSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ContinueUrl.MustNotBeNull("settings.ContinueUrl");

            return settings.ContinueUrl;
        }

        public override CallbackResult ProcessCallback(OrderReadOnly order, HttpRequestBase request, WorldpayAccessOneTimeSettings settings)
        {
            return new CallbackResult
            {
                TransactionInfo = new TransactionInfo
                {
                    AmountAuthorized = order.TotalPrice.Value.WithTax,
                    TransactionFee = 0m,
                    TransactionId = Guid.NewGuid().ToString("N"),
                    PaymentStatus = PaymentStatus.Authorized
                }
            };
        }
    }
}
