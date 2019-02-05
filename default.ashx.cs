using System;
using System.Data;
using System.Net;
using System.Web;
using System.Collections;
using System.Collections.Generic;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Threading;
using System.Globalization;

using IT.Services.Text;

namespace ToText
{
    /// <summary>
    /// Summary description for $codebehindclassname$
    /// </summary>
    [WebService(Namespace = "http://www.vasiliadis.eu/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class _default : IHttpHandler
    {
        protected delegate void ProcessMethod(HttpContext context);
        public bool IsReusable { get { return false; } }

        protected static Dictionary<string, ProcessMethod> methods = new Dictionary<string, ProcessMethod>();

        protected bool VerifyApiKey(HttpContext context)
        {
            string apikey = context.Request["key"];
            if (string.IsNullOrEmpty(apikey))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.StatusDescription = "Forbidden";
                return false;
            }

            return true;
        }


        public void ProcessRequest(HttpContext context)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string HttpMethod = context.Request.HttpMethod.ToUpperInvariant();

            if (methods.Count == 0)
            {
                methods.Add(WebRequestMethods.Http.Get, Process_GET);
                methods.Add(WebRequestMethods.Http.Post, DummyProcess);
                methods.Add("DELETE", Process_DELETE);

                methods.Add(WebRequestMethods.Http.Connect, DummyProcess);
                methods.Add(WebRequestMethods.Http.Head, DummyProcess);
                methods.Add(WebRequestMethods.Http.Put, DummyProcess);
            }

            if (methods.ContainsKey(HttpMethod))
                methods[HttpMethod].Invoke(context);
            else
                DummyProcess(context);
        }

        protected void DummyProcess(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.NotAcceptable; //406;
            context.Response.StatusDescription = "Not Acceptable";
        }

        protected void Process_DELETE(HttpContext context)
        {
            if (!VerifyApiKey(context))
                return;

            DummyProcess(context);
        }

        protected void Process_GET(HttpContext context)
        {
            if (!VerifyApiKey(context)) return;

            string format = context.Request["format"];
            if (string.IsNullOrEmpty(format))
                format = "json";
            else
                format = format.ToLower();

            string value = context.Request["value"];
            if (string.IsNullOrEmpty(value))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                //context.Response.AddHeader("Access-Control-Allow-Origin", "http://www.vasiliadis.eu");
                return;
            }

            if (!Written.ValidValueFormat(value))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotAcceptable; //406;
                context.Response.StatusDescription = "Not Acceptable";
                return;
            }

            SexForm sexform = SexForm.Neutral;
            {
                string strform = context.Request["sex"];
                if (string.IsNullOrEmpty(strform))
                    sexform = SexForm.Neutral;
                else
                {
                    strform = strform.Trim().ToLower();
                    switch (strform)
                    {
                        case "male":
                        case "0":
                            sexform = SexForm.Male;
                            break;
                        case "female":
                        case "1":
                            sexform = SexForm.Female;
                            break;
                        case "neutral":
                        case "2":
                        default:
                            sexform = SexForm.Neutral;
                            break;
                    }
                }
            }

            value = value.Trim();
            double dblvalue = 0;
            bool ok = false;
            try
            {
                ok = double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out dblvalue);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Assert(false, e.Message);
                ok = false;
            }

            if (!ok)
            {
                DummyProcess(context);
                return;
            }

            dblvalue = Math.Round(dblvalue, 2);
            //string retvalue = IT.Services.Text.Written.GetText(dblvalue, SexForm.Neutral);

            string[] names = null;
            bool showCurrency = false;
            if (!string.IsNullOrEmpty(context.Request["names"]))
            {
                showCurrency = true;
                names = context.Request["names"].Split(",".ToCharArray());
                for (int i = names.Length; i < 4; i++) { names[i] = names[i - 1]; }
            }

            //string retvalue = Written.GetText(value, sexform, true, showCurrency);
            string retvalue = Written.GetText(value, sexform, true, true);

            System.Text.StringBuilder str = new System.Text.StringBuilder(retvalue);
            if (showCurrency)
            {
                retvalue = str
                    .Replace("€€", names[1])
                    .Replace("¢¢", names[3])
                    .Replace("€",  names[0])
                    .Replace("¢",  names[2])
                    .ToString();
            }
            else
            {
                retvalue = str
                    .Replace(" €€", string.Empty)
                    .Replace(" ¢¢", string.Empty)
                    .Replace(" €",  string.Empty)
                    .Replace(" ¢",  string.Empty)
                    .ToString();
            }

            if ((format == "json") || string.IsNullOrEmpty(format))
            {
                context.Response.ContentType = "application/json";
                //context.Response.AddHeader("Access-Control-Allow-Origin", "http://www.vasiliadis.eu");
                string json = string.Format("{{\"text\":\"{0}\"}}", retvalue);
                context.Response.Write(json);
            }
            else if (format == "text")
            {
                context.Response.ContentType = "text/plain";
                //context.Response.AddHeader("Access-Control-Allow-Origin", "http://www.vasiliadis.eu");
                context.Response.Write(retvalue);
            }
            else if (format == "xml")
            {
                context.Response.ContentType = "application/xml";
                //context.Response.AddHeader("Access-Control-Allow-Origin", "http://www.vasiliadis.eu");
                string xml = string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><result><text>{0}</text></result>", retvalue);
                context.Response.Write(xml);
            }
            else
            {
                DummyProcess(context);
            }
        }
    }
}
