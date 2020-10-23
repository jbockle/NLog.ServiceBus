using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.ServiceBus;
using NLog.Common;

namespace NLog.ServiceBus
{
    internal class MessageReflectionHelper
    {
        private readonly Dictionary<Type, Func<string, object>> _converterMethodMap
            = new Dictionary<Type, Func<string, object>>
            {
                [typeof(string)] = value => value,
                [typeof(DateTime)] = value => DateTime.TryParse(value, out var result)
                    ? (object)result.ToUniversalTime()
                    : null,
                [typeof(TimeSpan)] = value =>
                {
                    if (TimeSpan.TryParse(value, out var result))
                    {
                        return result;
                    }

                    return null;
                },
            };

        public MessageReflectionHelper()
        {
            Properties = typeof(Message)
                .GetRuntimeProperties()
                .Where(prop => prop.CanWrite);
        }

        public IEnumerable<PropertyInfo> Properties { get; }


        public bool TrySetProperty(string key, string value, Message on)
        {
            var property = Properties
                .FirstOrDefault(prop => prop.Name.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (property == null)
            {
                InternalLogger.Warn(
                    "A property with name '{0}' was not found and was not set", key);

                return false;
            }

            if (!TryConvertValue(property, value, out var convertedValue))
            {
                return false;
            }

            property.SetValue(on, convertedValue);

            return true;
        }

        private bool TryConvertValue(PropertyInfo property, string value, out object outValue)
        {
            outValue = _converterMethodMap[property.PropertyType].Invoke(value);

            return outValue != null;
        }
    }
}