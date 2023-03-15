using System.Collections.Generic;

namespace Conan.VisualStudio.Core
{
    public class ConanSettings
    {
        public string Version { get; set; }
        public List<ConanCommand> ConanCommands { get; set; }
    }

    public class ConanCommand
    {
        public string Name { get; set; }
        public string Args { get; set; }
    }

    public enum ConanGeneratorType
    {
        visual_studio,
        visual_studio_multi,
        MSBuildDeps
    }

    public enum ConanBuildType
    {
        always,
        never,
        missing,
        cascade,
        outdated,
        none
    }

    public enum ConanTriggerType
    {
        manual,
        automatic
    }
}
