using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

partial class Build
{
    AbsolutePath PublicApiFile => RootDirectory / "PUBLIC_API.md";

    Target GeneratePublicApi => _ => _
        .Executes(() =>
        {
            var types = typeof(FalloutBuild).Assembly
                .GetTypes()
                .SelectMany(x => x.DescendantsAndSelf(y => y.GetNestedTypes()))
                .Where(x => x.IsPublic || x.IsNestedPublic)
                .Distinct()
                .OrderBy(x => x.FullName).ToList();

            var builder = new StringBuilder();

            builder
                .AppendLine("# Public API")
                .AppendLine()
                .AppendLine("## Namespaces & Types")
                .AppendLine();

            var groups = types.GroupBy(x => x.Namespace);

            foreach (var group in groups)
            {
                builder.AppendLine($"### {group.Key}");
                builder.AppendLine();
                group.ForEach(x => builder.AppendLine($"- {x.GetDisplayName()}"));
                builder.AppendLine();
            }

            builder
                .AppendLine("## Types & Methods")
                .AppendLine();

            foreach (var type in types)
            {
                builder
                    .AppendLine($"### {type.Namespace}.{type.GetDisplayName()}")
                    .AppendLine();

                var memberInfos = type
                    .GetMembers(ReflectionUtility.All | BindingFlags.DeclaredOnly);

                bool DefaultFilter(MemberInfo member)
                {
                    if (member is PropertyInfo)
                    {
                        return false;
                    }

                    if (member is Type && !member.IsPublic())
                    {
                        return false;
                    }

                    if (!(member.IsPublic() || (member.IsFamily() && !member.DeclaringType.NotNull().IsSealed)))
                    {
                        return false;
                    }

                    if (member is FieldInfo { IsSpecialName: true })
                    {
                        return false;
                    }

                    return true;
                }

                var members = memberInfos
                    .Where(DefaultFilter)
                    .OrderByDescending(x => x is FieldInfo)
                    .ThenByDescending(x => x is ConstructorInfo)
                    .ThenByDescending(x => x is MethodInfo)
                    .ThenByDescending(x => x.Name.StartsWith("get_") || x.Name.StartsWith("set_"))
                    .ThenBy(x => x.Name);

                foreach (var member in members)
                {
                    builder.AppendLine($"- {member.GetDisplayText()}");
                }

                builder.AppendLine();
            }

            PublicApiFile.WriteAllText(builder.ToString());
        });
}
