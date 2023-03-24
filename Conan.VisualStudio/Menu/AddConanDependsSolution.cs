using System.ComponentModel.Design;
using System.Threading.Tasks;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;
using System;
using EnvDTE;
using EnvDTE80;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.OLE.Interop;
using Conan.VisualStudio.Core;

namespace Conan.VisualStudio.Menu
{
    /// <summary>Command handler.</summary>
    internal sealed class AddConanDependsSolution : MenuCommandBase
    {
        protected override int CommandId => PackageIds.AddConanDependsSolutiontId;

        private readonly IVcProjectService _vcProjectService;
        private readonly Core.IErrorListService _errorListService;
        private readonly IConanService _conanService;

        public AddConanDependsSolution(
            IMenuCommandService commandService,
            Core.IErrorListService errorListService,
            IVcProjectService vcProjectService,
            IConanService conanService) : base(commandService, errorListService)
        {
            _vcProjectService = vcProjectService;
            _errorListService = errorListService;
            _conanService = conanService;
        }

        protected internal override async Task MenuItemCallbackAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _errorListService.Clear();

            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var projects = GetSolutionProjects(dte.Solution);

            if (!await IntegrateConanPropsAsync(projects))
            {
                Logger.Log($"[Conan.VisualStudio] ========== Build failed ==========");
                return;
            }

            Logger.Log($"[Conan.VisualStudio] ========== Build succeeded, {projects.Count} updated ==========");
            await TaskScheduler.Default;
        }

        private async Task<bool> IntegrateConanPropsAsync(List<Project> projects)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var conanfiles = new HashSet<string>();

            foreach (Project project in projects)
            {
                Logger.Log($"[Conan.VisualStudio] Processing {project.Name} project...");
                var vcProject = _vcProjectService.AsVCProject(project);
                var conanProject = await _conanService.PrepareConanProjectAsync(vcProject);
                if (conanProject == null)
                {
                    return false;
                }

                if (!conanfiles.Contains(conanProject.Path) && !await _conanService.InstallAsync(conanProject))
                {
                    return false;
                }

                conanfiles.Add(conanProject.Path);
                await _conanService.IntegrateAsync(vcProject);
            }
            return true;
        }

        private List<Project> GetSolutionProjects(Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            List<Project> projects = new List<Project>();
            foreach (Project project in solution.Projects)
            {
                AddProjects(projects, project);
            }
            return projects;
        }

        private void AddProjects(List<Project> projects, Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (project == null)
            {
                return;
            }

            if (_vcProjectService.IsConanProject(project))
            {
                projects.Add(project);
                return;
            }

            AddProjects(projects, project.ProjectItems);
        }

        private void AddProjects(List<Project> projects, ProjectItems items)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (items == null)
            {
                return;
            }

            foreach (ProjectItem item in items)
            {
                AddProjects(projects, item.SubProject);
                AddProjects(projects, item.ProjectItems);
            }
        }
    }
}
