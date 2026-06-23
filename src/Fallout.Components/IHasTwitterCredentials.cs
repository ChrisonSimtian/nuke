using System;
using System.Linq;
using Fallout.Common;

namespace Fallout.Components;

[ParameterPrefix(Twitter)]
public interface IHasTwitterCredentials : IFalloutBuild
{
    const string Twitter = nameof(Twitter);

    [Parameter] [Secret] string ConsumerKey => TryGetValue(() => ConsumerKey);
    [Parameter] [Secret] string ConsumerSecret => TryGetValue(() => ConsumerSecret);
    [Parameter] [Secret] string AccessToken => TryGetValue(() => AccessToken);
    [Parameter] [Secret] string AccessTokenSecret => TryGetValue(() => AccessTokenSecret);
}