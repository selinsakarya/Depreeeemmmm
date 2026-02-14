using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Web;

namespace Depreeeemmmm.Extensions;

public static class HttpRequestExtensions
{
        public static string ToQueryString(this object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            List<string> result = new List<string>();

            IEnumerable<PropertyInfo> props = obj.GetType().GetProperties().Where(p => p.GetValue(obj, null) != null);

            foreach (PropertyInfo p in props)
            {
                if (Attribute.IsDefined(p, typeof(IgnoreDataMemberAttribute)))
                {
                    continue;
                }

                string name = p.Name;
                if (Attribute.IsDefined(p, typeof(JsonPropertyNameAttribute)))
                {
                    dynamic attribute = p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false).First();
                    name = attribute.Name;
                }

                object value = p.GetValue(obj, null);

                if (value is Enum)
                {
                    value = value.GetEnumMemberValue() ?? value;

                    result.Add($"{name}={HttpUtility.UrlEncode(value.ToString())}");
                }
                else if (value is ICollection enumerable)
                {
                    result.AddRange(from object v in enumerable select $"{name}={HttpUtility.UrlEncode(v.ToString())}");
                }
                else if (value != null)
                {
                    result.Add(value is DateTime ? $"{name}={HttpUtility.UrlEncode($"{value:yyyy-MM-dd HH:mm:ss.fff}")}" : $"{name}={HttpUtility.UrlEncode(value.ToString())}");
                }
            }

            return string.Join("&", result.ToArray());
        }

        private static string? GetEnumMemberValue(this object value)
        {
            if (value is Enum)
            {
                FieldInfo fieldInfo = value.GetType().GetField(value.ToString());

                EnumMemberAttribute attribute = ((EnumMemberAttribute[])fieldInfo.GetCustomAttributes(typeof(EnumMemberAttribute), false)).FirstOrDefault();

                return attribute?.Value;
            }

            return null;
        }
}