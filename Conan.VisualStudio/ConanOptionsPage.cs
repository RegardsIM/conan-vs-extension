using System;
using System.ComponentModel;
using System.Windows.Forms;
using Conan.VisualStudio.Core;
using Microsoft.VisualStudio.Shell;

namespace Conan.VisualStudio
{
    public class ConanOptionsPage : DialogPage
    {
        private string _conanExecutablePath;
        private string _conanInstallationPath;
        private bool? _conanInstallOnlyActiveConfiguration;
        private ConanGeneratorType? _conanGenerator;
        private bool? _conanInstallAutomatically;
        private ConanBuildType? _conanBuild;
        private bool? _conanUpdate;
        private bool? _conanAddPropsToProjects;

        protected override void OnApply(PageApplyEventArgs e)
        {
            if (!ValidateConanExecutableAndShowMessage(_conanExecutablePath))
            {
                e.ApplyBehavior = ApplyKind.Cancel;
            }
            else
            {
                base.OnApply(e);
            }
        }

        private bool ValidateConanExecutableAndShowMessage(string exe)
        {
            if (!ConanPathHelper.ValidateConanExecutable(exe, out string errorMessage))
            {
                MessageBox.Show(errorMessage, "Conan extension: invalid conan executable",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        [Category("Conan")]
        [DisplayName("Executable path")]
        [Description(@"Path to the Conan executable file, like C:\Python27\Scripts\conan.exe")]
        public string ConanExecutablePath
        {
            get => _conanExecutablePath ?? (_conanExecutablePath = ConanPathHelper.DetermineConanPathFromEnvironment());
            set => _conanExecutablePath = value;
        }

        [Category("Arguments")]
        [DisplayName("Install directory (--install-folder)")]
        [Description(@"Path to the conan installation directory, may use macro like $(OutDir) or $(ProjectDir). Absolute or relative to the project directory.")]
        public string ConanInstallationPath
        {
            get => _conanInstallationPath ?? (_conanInstallationPath = "$(SolutionDir)conan");
            set => _conanInstallationPath = value;
        }

        [Category("Conan")]
        [DisplayName("Generate only active configuration")]
        [Description(@"Generate only active configuration or all configurations")]
        public bool ConanInstallOnlyActiveConfiguration
        {
            get => _conanInstallOnlyActiveConfiguration ?? true;
            set => _conanInstallOnlyActiveConfiguration = value;
        }

        [Category("Arguments")]
        [DisplayName("Generator (-g)")]
        [Description(@"Conan generator to use")]
        public ConanGeneratorType ConanGenerator
        {
            get => _conanGenerator ?? ConanGeneratorType.visual_studio_multi;
            set => _conanGenerator = value;
        }

        [Category("Conan")]
        [DisplayName("Generate conan props automatically")]
        [Description(@"Generate conan dependencies automatically on solution load")]
        public bool ConanInstallAutomatically
        {
            get => _conanInstallAutomatically ?? false;
            set => _conanInstallAutomatically = value;
        }

        [Category("Arguments")]
        [DisplayName("Build policy (--build)")]
        [Description(@"--build argument (always, never, missing, cascade, outdated or none)")]
        public ConanBuildType ConanBuild
        {
            get => _conanBuild ?? ConanBuildType.missing;
            set => _conanBuild = value;
        }

        [Category("Arguments")]
        [DisplayName("Add update flag (--update)")]
        [Description(@"Check updates exist from upstream remotes")]
        public bool ConanUpdate
        {
            get => _conanUpdate ?? false;
            set => _conanUpdate = value;
        }

        [Category("Conan")]
        [DisplayName("Add generated props to projects")]
        [Description(@"Add generated props to projects as a dependency")]
        public bool ConanAddPropsToProjects
        {
            get => _conanAddPropsToProjects ?? true;
            set => _conanAddPropsToProjects = value;
        }
    }
}
