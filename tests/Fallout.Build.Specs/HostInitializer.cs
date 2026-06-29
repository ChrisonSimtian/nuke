using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Fallout.Common.Utilities;

namespace Fallout.Common.Specs;

public static class HostInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        FalloutBuild.Host = new SilentHost();
    }

    private class SilentHost : Host
    {
        protected internal override IDisposable WriteBlock(string text)
        {
            return DelegateDisposable.CreateBracket();
        }
    }
}
