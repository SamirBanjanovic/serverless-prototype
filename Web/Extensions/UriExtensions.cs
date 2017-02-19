using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Serverless.Web.Extensions
{
    public static class UriExtensions
    {
        public static Uri Append(this Uri baseUri, string suffix)
        {
            return new Uri(Path.Combine(baseUri.AbsoluteUri, suffix));
        }
    }
}