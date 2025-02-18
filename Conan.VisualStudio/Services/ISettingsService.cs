using Conan.VisualStudio.Core;

namespace Conan.VisualStudio.Services
{
    public interface ISettingsService
    {
        /// <summary>Returns Conan executable path as defined in the project options.</summary>
        /// <returns>Executable path. May be <c>null</c> if Conan not found.</returns>
        string GetConanExecutablePath();

        /// <summary>Returns Conan installation path - to be used as target for the "conan install" command.</summary>
        /// <returns>Installation path. Might contain visual studio macro definitions (like $(OutDir)). Relative path is evaluated against project directory</returns>
        string GetConanInstallationPath();

        ConanUsedConfigurationType GetConanUsedConfiguration();

        /// <summary>
        /// returns default conan generator, either visual_studio, or visual_studio_multi
        /// </summary>
        /// <returns>value of default conan generator type</returns>
        ConanGeneratorType GetConanGenerator();

        ConanTriggerType GetConanTrigger();

        ConanBuildType GetConanBuild();
        bool GetConanUpdate();

        bool GetConanAddPropsToProjects();
    }
}
