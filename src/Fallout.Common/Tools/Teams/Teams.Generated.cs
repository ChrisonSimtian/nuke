// Generated from https://github.com/Fallout-build/Fallout/blob/main/src/Fallout.Common/Tools/Teams/Teams.json

using Fallout.Common;
using Fallout.Common.Tooling;
using Fallout.Common.Tools;
using Fallout.Common.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Fallout.Common.Tools.Teams;

#region TeamsMessage
/// <summary>Used within <see cref="TeamsTasks"/>.</summary>
[ExcludeFromCodeCoverage]
public partial class TeamsMessage : Options
{
    /// <summary></summary>
    [JsonPropertyName("title")] public string Title => Get<string>(() => Title);
    /// <summary></summary>
    [JsonPropertyName("text")] public string Text => Get<string>(() => Text);
    /// <summary></summary>
    [JsonPropertyName("themeColor")] public string ThemeColor => Get<string>(() => ThemeColor);
}
#endregion
#region TeamsMessageExtensions
/// <summary>Used within <see cref="TeamsTasks"/>.</summary>
[ExcludeFromCodeCoverage]
public static partial class TeamsMessageExtensions
{
    #region Title
    /// <inheritdoc cref="TeamsMessage.Title"/>
    [Builder(Type = typeof(TeamsMessage), Property = nameof(TeamsMessage.Title))]
    public static T SetTitle<T>(this T o, string v) where T : TeamsMessage => o.Modify(b => b.Set(() => o.Title, v));
    /// <inheritdoc cref="TeamsMessage.Title"/>
    [Builder(Type = typeof(TeamsMessage), Property = nameof(TeamsMessage.Title))]
    public static T ResetTitle<T>(this T o) where T : TeamsMessage => o.Modify(b => b.Remove(() => o.Title));
    #endregion
    #region Text
    /// <inheritdoc cref="TeamsMessage.Text"/>
    [Builder(Type = typeof(TeamsMessage), Property = nameof(TeamsMessage.Text))]
    public static T SetText<T>(this T o, string v) where T : TeamsMessage => o.Modify(b => b.Set(() => o.Text, v));
    /// <inheritdoc cref="TeamsMessage.Text"/>
    [Builder(Type = typeof(TeamsMessage), Property = nameof(TeamsMessage.Text))]
    public static T ResetText<T>(this T o) where T : TeamsMessage => o.Modify(b => b.Remove(() => o.Text));
    #endregion
    #region ThemeColor
    /// <inheritdoc cref="TeamsMessage.ThemeColor"/>
    [Builder(Type = typeof(TeamsMessage), Property = nameof(TeamsMessage.ThemeColor))]
    public static T SetThemeColor<T>(this T o, string v) where T : TeamsMessage => o.Modify(b => b.Set(() => o.ThemeColor, v));
    /// <inheritdoc cref="TeamsMessage.ThemeColor"/>
    [Builder(Type = typeof(TeamsMessage), Property = nameof(TeamsMessage.ThemeColor))]
    public static T ResetThemeColor<T>(this T o) where T : TeamsMessage => o.Modify(b => b.Remove(() => o.ThemeColor));
    #endregion
}
#endregion
