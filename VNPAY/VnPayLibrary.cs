using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace LanguageCenter.VNPAY
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly SortedList<string, string> responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                requestData[key] = value;
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(key) && value != null)
            {
                responseData[key] = value;
            }
        }

        public string GetResponseData(string key)
        {
            return responseData.ContainsKey(key) ? responseData[key] : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var signData = GetRequestSignData();
            var secureHash = HmacSHA512(hashSecret, signData);

            return baseUrl + "?" + signData + "&vnp_SecureHash=" + secureHash;
        }

        public bool ValidateSignature(string inputHash, string hashSecret)
        {
            var signData = GetResponseSignData();
            var computedHash = HmacSHA512(hashSecret, signData);

            return string.Equals(computedHash, inputHash, StringComparison.OrdinalIgnoreCase);
        }

        public static string FormatAmount(decimal amount)
        {
            return ((long)(amount * 100)).ToString(CultureInfo.InvariantCulture);
        }

        public string GetRequestDebugQuery()
        {
            return GetRequestSignData();
        }

        public string GetRequestSignData()
        {
            return BuildQueryString(requestData);
        }

        public string GetResponseSignData()
        {
            var filteredData = new SortedList<string, string>(new VnPayCompare());

            foreach (var item in responseData)
            {
                if (!item.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                    && !item.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                {
                    filteredData[item.Key] = item.Value;
                }
            }

            return BuildQueryString(filteredData);
        }

        private static string BuildQueryString(SortedList<string, string> data)
        {
            var parts = data
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .Select(item =>
                {
                    return WebUtility.UrlEncode(item.Key)
                        + "="
                        + WebUtility.UrlEncode(item.Value);
                });

            return string.Join("&", parts);
        }

        private static string HmacSHA512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key ?? string.Empty);
            var inputBytes = Encoding.UTF8.GetBytes(inputData ?? string.Empty);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                return BitConverter.ToString(hashValue).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private class VnPayCompare : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return string.CompareOrdinal(x, y);
            }
        }
    }
}
