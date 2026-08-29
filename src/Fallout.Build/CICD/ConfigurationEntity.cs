using Fallout.Common.Utilities;

namespace Fallout.Common.CI;

public abstract class ConfigurationEntity
{
    public abstract void Write(CustomFileWriter writer);
}
