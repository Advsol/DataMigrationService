using System;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace Asi.DataMigrationService.Core
{
    public static class Utility
    {
        //Unused Method
        public static bool TryConvert(object value, Type destinationType, out object result)
        {
            return TryConvert(value, destinationType, string.Empty, out result);
        }
        public static bool TryConvert(object value, Type destinationType, string culture, out object result)
        {
            if (destinationType == null) throw new ArgumentNullException(nameof(destinationType));
            try
            {
                if (value != null)
                {
                    if (destinationType == typeof(string))
                    {
                        result = value;
                        return true;
                    }

                    var cultureInfo = CultureInfo.GetCultureInfo(culture);

                    var converter = TypeDescriptor.GetConverter(destinationType);
                    if (converter.CanConvertFrom(value.GetType()))
                    {
                        result = converter.ConvertFrom(null, cultureInfo, value);
                        return true;
                    }

                    if (destinationType.IsEnum && value is int i)
                    {
                        result = Enum.ToObject(destinationType, i);
                        return true;
                    }

                    if (destinationType == typeof(byte[])) 
                    {
                        ASCIIEncoding encoding = new ASCIIEncoding();
                        result = encoding.GetBytes((string)value);
                        return true;
                    }

                    if (!destinationType.IsInstanceOfType(value) && value is IConvertible)
                    {
                        // Convert.ChangeType can't convert to nullable types, so if the destination type
                        // is nullable, convert to the non-nullable base type.
                        var nonNullableType = Nullable.GetUnderlyingType(destinationType);
                        if (nonNullableType != null)
                            destinationType = nonNullableType;
                        result = System.Convert.ChangeType(value, destinationType, cultureInfo);
                        return true;
                    }
                    result = null;
                    return false;
                }

                result = value;
                return true;
            }
            catch (FormatException )
            {
                result = null;
                return false;
            }
        }

        //Unused Method
        public static bool TryConvert<T>(object value, out T result)
        {
            return TryConvert<T>(value, string.Empty, out result);
        }

        public static bool TryConvert<T>(object value, string culture, out T result)
        {
            if (TryConvert(value, typeof(T), culture, out var r1))
            {
                result = (T)r1;
                return true;
            }
            result = default;
            return false;
        }
    }
}
