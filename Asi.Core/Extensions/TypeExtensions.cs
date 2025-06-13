using System;
using System.Linq;
using System.Reflection;

namespace Asi.DataMigrationService.Core.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>   A Type extension method that gets generic method. </summary>
        ///
        /// <param name="type">         The type. </param>
        /// <param name="name">         The name. </param>
        /// <param name="genericArgs">  The generic arguments. </param>
        /// <param name="types">        The types. </param>
        ///
        /// <returns>   The generic method. </returns>
        public static MethodInfo GetGenericMethod(this Type type, string name, Type[] genericArgs, Type[] types)
        {
            foreach (var m in type.GetMethods())
            {
                if (m.Name == name)
                {
                    var pa = m.GetParameters();
                    if (pa.Length == types.Length)
                    {
                        var c = m.MakeGenericMethod(genericArgs);
                        if (c.GetParameters().Select(p => p.ParameterType).SequenceEqual(types))
                            return c;
                    }
                }
            }

            return null;
        }
    }
}
