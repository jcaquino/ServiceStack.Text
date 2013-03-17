﻿using System;
using System.IO;
using System.Net;

namespace ServiceStack.Text
{
    public static class WebRequestExtensions
    {
        public const string Json = "application/json";
        public const string Xml = "application/xml";
        public const string FormUrlEncoded = "application/x-www-form-urlencoded";
        public const string MultiPartFormData = "multipart/form-data";
        
        public static string GetJsonFromUrl(this string url, 
            Action<HttpWebRequest> requestilter = null, Action<HttpWebResponse> responseFilter = null)
        {
            return url.GetStringFromUrl(Json, requestilter, responseFilter);
        }

        public static string GetXmlFromUrl(this string url,
            Action<HttpWebRequest> requestilter = null, Action<HttpWebResponse> responseFilter = null)
        {
            return url.GetStringFromUrl(Xml, requestilter, responseFilter);
        }

        public static string GetStringFromUrl(this string url, string acceptContentType = "*/*",
            Action<HttpWebRequest> requestilter = null, Action<HttpWebResponse> responseFilter = null)
        {
            return SendStringToUrl(url, acceptContentType: acceptContentType, requestilter: requestilter, responseFilter: responseFilter);
        }

        public static string PostToUrl(this string url, object formData = null, string acceptContentType = "*/*",
            Action<HttpWebRequest> requestilter = null, Action<HttpWebResponse> responseFilter = null)
        {
            string postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

            return SendStringToUrl(url, method: "POST",
                contentType: FormUrlEncoded, requestBody: postFormData,
                acceptContentType: acceptContentType, requestilter: requestilter, responseFilter: responseFilter);
        }

        public static string PutToUrl(this string url, object formData = null, string acceptContentType = "*/*",
            Action<HttpWebRequest> requestilter = null, Action<HttpWebResponse> responseFilter = null)
        {
            string postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

            return SendStringToUrl(url, method: "PUT",
                contentType: FormUrlEncoded, requestBody: postFormData,
                acceptContentType: acceptContentType, requestilter: requestilter, responseFilter: responseFilter);
        }

        public static string DeleteFromUrl(this string url, string acceptContentType = "*/*",
            Action<HttpWebRequest> requestilter = null, Action<HttpWebResponse> responseFilter = null)
        {
            return SendStringToUrl(url, method: "DELETE", acceptContentType: acceptContentType, requestilter: requestilter, responseFilter: responseFilter);
        }

        public static string OptionsFromUrl(this string url, string acceptContentType = "*/*",
            Action<HttpWebRequest> requestilter = null, Action<HttpWebResponse> responseFilter = null)
        {
            return SendStringToUrl(url, method: "OPTIONS", acceptContentType: acceptContentType, requestilter: requestilter, responseFilter: responseFilter);
        }

        public static string HeadFromUrl(this string url, string acceptContentType = "*/*",
            Action<HttpWebRequest> requestilter = null, Action<HttpWebResponse> responseFilter = null)
        {
            return SendStringToUrl(url, method: "HEAD", acceptContentType: acceptContentType, requestilter: requestilter, responseFilter: responseFilter);
        }

        public static string SendStringToUrl(this string url, string method = null,
            string requestBody = null, string contentType = null, string acceptContentType = "*/*",
            Action<HttpWebRequest> requestilter = null, Action<HttpWebResponse> responseFilter = null)
        {
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            if (method != null)
                webReq.Method = method;
            if (contentType != null)
                webReq.ContentType = contentType;

            webReq.Accept = acceptContentType;
            webReq.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            webReq.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            if (requestBody != null)
            {
                using (var reqStream = webReq.GetRequestStream())
                using (var writer = new StreamWriter(reqStream))
                {
                    writer.Write(requestBody);
                }
            }

            using (var webRes = webReq.GetResponse())
            using (var stream = webRes.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                if (responseFilter != null)
                {
                    responseFilter((HttpWebResponse)webRes);
                }
                return reader.ReadToEnd();
            }
        }

        public static byte[] SendBytesToUrl(this string url, string method = null,
            byte[] requestBody = null, string contentType = null, string acceptContentType = "*/*",
            Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
        {
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            if (method != null)
                webReq.Method = method;

            if (contentType != null)
                webReq.ContentType = contentType;

            webReq.Accept = acceptContentType;
            webReq.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            webReq.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            if (requestFilter != null)
            {
                requestFilter(webReq);
            }

            if (requestBody != null)
            {
                using (var req = webReq.GetRequestStream())
                {
                    req.Write(requestBody, 0, requestBody.Length);                    
                }
            }

            using (var webRes = webReq.GetResponse())
            {
                if (responseFilter != null)
                    responseFilter((HttpWebResponse)webRes);

                using (var stream = webRes.GetResponseStream())
                {
                    return stream.ReadFully();
                }
            }
        }

        public static bool Is200(this Exception ex)
        {
            var status = ex.GetStatus();
            return status >= HttpStatusCode.OK && status < HttpStatusCode.MultipleChoices;
        }

        public static bool Is300(this Exception ex)
        {
            var status = ex.GetStatus();
            return status >= HttpStatusCode.MultipleChoices && status < HttpStatusCode.BadRequest;
        }

        public static bool Is400(this Exception ex)
        {
            var status = ex.GetStatus();
            return status >= HttpStatusCode.BadRequest && status < HttpStatusCode.InternalServerError;
        }

        public static bool Is500(this Exception ex)
        {
            var status = ex.GetStatus();
            return status >= HttpStatusCode.InternalServerError && (int)status < 600;
        }

        public static bool IsBadRequest(this Exception ex)
        {
            return HasStatus(ex as WebException, HttpStatusCode.BadRequest);
        }

        public static bool IsNotFound(this Exception ex)
        {
            return HasStatus(ex as WebException, HttpStatusCode.NotFound);
        }

        public static bool IsUnauthorized(this Exception ex)
        {
            return HasStatus(ex as WebException, HttpStatusCode.Unauthorized);
        }

        public static bool IsForbidden(this Exception ex)
        {
            return HasStatus(ex as WebException, HttpStatusCode.Forbidden);
        }

        public static bool IsInternalServerError(this Exception ex)
        {
            return HasStatus(ex as WebException, HttpStatusCode.InternalServerError);
        }

        public static HttpStatusCode? GetResponseStatus(this string url)
        {
            try
            {
                var webReq = (HttpWebRequest)WebRequest.Create(url);
                using (var webRes = webReq.GetResponse())
                {
                    var httpRes = webRes as HttpWebResponse;
                    return httpRes != null ? httpRes.StatusCode : (HttpStatusCode?)null;
                }
            }
            catch (Exception ex)
            {
                return ex.GetStatus();
            }
        }

        public static HttpStatusCode? GetStatus(this Exception ex)
        {
            return GetStatus(ex as WebException);
        }

        public static HttpStatusCode? GetStatus(this WebException webEx)
        {
            if (webEx == null) return null;
            var httpRes = webEx.Response as HttpWebResponse;
            return httpRes != null ? httpRes.StatusCode : (HttpStatusCode?)null;
        }

        public static bool HasStatus(this WebException webEx, HttpStatusCode statusCode)
        {
            return GetStatus(webEx) == statusCode;
        }

    }
}