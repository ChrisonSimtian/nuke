using System;

namespace Nuke.Components;

[Obsolete(
    "Renamed to IHasNerdbankGitVersioning. The IHaz* names were legacy NUKE conventions; renamed for clarity in Fallout v11. This alias is shipped only in the Nuke.Components transition shim and will be removed in v12.")]
public interface IHazNerdbankGitVersioning : IHasNerdbankGitVersioning
{
}
