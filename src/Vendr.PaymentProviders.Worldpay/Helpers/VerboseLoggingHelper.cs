using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Vendr.PaymentProviders.Worldpay.Helpers
{
    public static class VerboseLoggingHelper
    {
        public static string ToFriendlyString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            var items = from kvp in dictionary
                select kvp.Key + "=" + kvp.Value;

            return "{" + string.Join(",", items) + "}";
        }

        public static string ToFriendlyString(this NameValueCollection nameValueCollection)
        {
            if (nameValueCollection == null)
                throw new ArgumentNullException("nameValueCollection");

            var nvc = nameValueCollection.AllKeys.SelectMany(nameValueCollection.GetValues, (k, v) => new { key = k, value = v });
            var items = from item in nvc
                       select item.key + "=" + item.value;

            return "{" + string.Join(",", items) + "}";
        }
    }
}