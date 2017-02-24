using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Serverless.Common.Extensions
{
    public static class GenericExtensions
    {
        public static string ToJson<T>(this T value)
        {
            return JsonConvert.SerializeObject(value: value);
        }
    }
}
