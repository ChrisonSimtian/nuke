using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Fallout.Common.Execution;
using Fallout.Common.Utilities;

namespace Fallout.Common;

public partial class Host
{
    internal static Host Instance { get; private set; }

    internal static Host Default =>
        AvailableTypes
            .OrderBy(x => x.IsAssignableTo(typeof(Terminal)))
            .ThenBy(x => x == typeof(Terminal))
            .Where(IsRunning)
            .Select(CreateHost)
            .First();

    internal static IEnumerable<Type> AvailableTypes
        => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.IsPublic)
            .Where(x => x.IsSubclassOf(typeof(Host)));

    private static bool IsRunning(Type hostType)
    {
        var propertyName = $"IsRunning{hostType.Name}";
        var member = hostType.GetProperty(propertyName, ReflectionUtility.Static);

        // A public Host subclass that doesn't follow the `IsRunning{Name}` convention is
        // not a runnable host — treat it as not-running rather than throwing. Host
        // auto-detection runs in FalloutBuild's static ctor, so an exception here crashes
        // every build that merely references such an assembly. The Nuke.* transition shim
        // emits exactly such a subclass (`Nuke.Common.Host`), which made any shim-referencing
        // build abort on startup. See Fallout.Canary#3.
        return member is not null && member.GetValue<bool>();
    }

    private static Host CreateHost(Type hostType)
    {
        return (Host)Activator.CreateInstance(hostType, nonPublic: true);
    }

    private class TypeConverter : System.ComponentModel.TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                var matchingHosts = AvailableTypes.Where(x => x.FullName.EndsWithOrdinalIgnoreCase(stringValue)).ToList();
                Assert.HasSingleItem(matchingHosts);
                return CreateHost(matchingHosts.Single());
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return false;
        }
    }
}
