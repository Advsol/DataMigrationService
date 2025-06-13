using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Asi.DataMigrationService.Lib.Publisher
{
    public class ImportTemplateProperty
    {
        public ImportTemplateProperty(PropertyInfo info)
        {
            PropertyInfo = info;
            Name = info.Name;
            Type = info.PropertyType;
            var ma = info.GetCustomAttribute<MaxLengthAttribute>();
            if (ma != null)
                MaxLength = ma.Length;
            IsRequired = info.GetCustomAttribute<RequiredAttribute>() != null;
        }
        public ImportTemplateProperty(string name)
        {
            Name = name;
            IsOtherColumn = true;
            Type = typeof(string);
        }

        public string Name { get; }
        public Type Type { get; }
        public bool IsRequired { get; }
        public int MaxLength { get; }
        public PropertyInfo PropertyInfo { get; }
        public bool IsOtherColumn { get; }
    }
}
