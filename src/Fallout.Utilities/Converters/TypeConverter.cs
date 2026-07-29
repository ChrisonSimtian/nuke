using System;
using System.ComponentModel;
using System.Globalization;
using Fallout.Common;
using Fallout.Common.IO;
using static Fallout.Common.IO.PathConstruction;

namespace Fallout.Utilities.Converters;

public class TypeConverter : System.ComponentModel.TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string stringValue)
        {
            return HasPathRoot(stringValue)
                ? (AbsolutePath)stringValue
                : EnvironmentInfo.WorkingDirectory / stringValue;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        return value is null
            ? null
            : base.ConvertFrom(context, culture, value);
    }
}
