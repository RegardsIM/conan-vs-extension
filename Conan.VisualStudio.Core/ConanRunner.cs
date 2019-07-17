using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Conan.VisualStudio.Core
{
    public class ConanRunner
    {
        private readonly ConanSettings _conanSettings;
        private readonly string _executablePath;

        public ConanRunner(ConanSettings conanSettings, string executablePath)
        {
            _conanSettings = conanSettings;
            _executablePath = executablePath;
        }

        private string Escape(string arg) =>
            arg.Contains(" ") ? $"\"{arg}\"" : arg;

        private string BuildOptions(ConanBuildType build, bool update)
        {
            string options = "";
            if (build != ConanBuildType.none)
            {
                if (build == ConanBuildType.always)
                    options += " --build";
                else
                    options += " --build=" + build.ToString();
            }
            if (update)
            {
                options += " --update";
            }
            return options;
        }

        public ProcessStartInfo Install(ConanProject project, ConanConfiguration configuration, ConanGeneratorType generator, ConanBuildType build, bool update, Core.IErrorListService errorListService)
        {
            string ProcessArgument(string name, string value) => $"-s {name}={Escape(value)}";

            var arguments = string.Empty;

            string profile = project.getProfile(configuration, errorListService);
            if (profile != null)
            {
                string generatorName = generator.ToString();
                arguments = $"install {Escape(project.Path)} " +
                            $"-g {generatorName} " +
                            $"--install-folder {Escape(configuration.InstallPath)} " +
                            $"--profile {Escape(profile)}" +
                            $"{BuildOptions(build, update)}";

            }
            else if (_conanSettings != null)
            {
                var installConfig = _conanSettings.ConanCommands.FirstOrDefault(c => c.Name.Equals("install"));
                arguments = installConfig.Args;
            }
            else
            {
                string generatorName = generator.ToString();
                var settingValues = new[]
                {
                    ("arch", configuration.Architecture),
                    ("build_type", configuration.BuildType),
                    ("compiler.toolset", configuration.CompilerToolset),
                    ("compiler.version", configuration.CompilerVersion),
                };
                if (configuration.RuntimeLibrary != null)
                {
                    settingValues = settingValues.Concat(new[] { ("compiler.runtime", configuration.RuntimeLibrary) }).ToArray();
                }

                var settings = string.Join(" ", settingValues.Where(pair => pair.Item2 != null).Select(pair =>
                {
                    var (key, value) = pair;
                    return ProcessArgument(key, value);
                }));
                arguments = $"install {Escape(project.Path)} " +
                            $"-g {generatorName} " +
                            $"--install-folder {Escape(configuration.InstallPath)} " +
                            $"{settings} {BuildOptions(build, update)}";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                Arguments = arguments,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(project.Path),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            return startInfo;
        }
    }
}
