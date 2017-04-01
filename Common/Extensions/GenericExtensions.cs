using System;
using Newtonsoft.Json;

namespace Serverless.Common.Extensions
{
    public static class GenericExtensions
    {
        public static string ToJson<T>(this T value)
        {
            return JsonConvert.SerializeObject(value: value);
        }

        public static T FromJson<T>(this string value)
        {
            return JsonConvert.DeserializeObject<T>(value: value);
        }
    }
}
