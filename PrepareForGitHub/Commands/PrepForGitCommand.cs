//------------------------------------------------------------------------------
// <copyright file="PrepForGitCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PrepareForGitHub.Shared.Commands;
using PrepareForGitHub.Shared.Helpers;
using PrepareForGitHub.VsixManifest.Parser;

namespace PrepareForGitHub
{
    /// <summary>
    /// Command handler
    /// </summary>
    sealed class PrepForGit : BaseCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("8401d26a-522c-4302-8e2e-28fd6315ebed");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        Package _package;

        static readonly string[] _visible = { "CHANGELOG.md", "README.md" };

        /// <summary>
        /// Initializes a new instance of the <see cref="PrepForGit"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private PrepForGit(Package package) : base(package)
        {
            //if (package == null)
            //{
            //    throw new ArgumentNullException("package");
            //}

            this._package = package;

            //OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            //if (commandService != null)
            //{
            //    var menuCommandID = new CommandID(CommandSet, CommandId);
            //    var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
            //    commandService.AddCommand(menuItem);
            //}
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PrepForGit Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this._package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new PrepForGit(package);
        }

        protected override void SetupCommands()
        {
            AddCommand(PackageGuids.guidPrepForGitPackageCmdSet, PackageIds.PrepForGitId, MenuItemCallbackAsync, BeforeQueryStatus);
        }

        void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;

            var solutionHasVsixProjects = ProjectHelpers.GetAllProjectsInSolution().Any(p => p.IsExtensibilityProject());

            button.Enabled = button.Visible = solutionHasVsixProjects;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void MenuItemCallbackAsync(object sender, EventArgs e)
        {
            //string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            //string title = "PrepForGit";

            //// Show a message box to prove we were here
            //VsShellUtilities.ShowMessageBox(
            //    this.ServiceProvider,
            //    message,
            //    title,
            //    OLEMSGICON.OLEMSGICON_INFO,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            if (!UserWantToProceed())
                return;

            string solutionRoot = Path.GetDirectoryName(DTE.Solution.FullName);
            var manifestFile = Directory.EnumerateFiles(solutionRoot, "*.vsixmanifest", SearchOption.AllDirectories).FirstOrDefault();
            var manifest = await VsixManifestParser.FromFileAsync(manifestFile);

            string assembly = Assembly.GetExecutingAssembly().Location;
            string root = Path.GetDirectoryName(assembly);
            string dir = Path.Combine(root, "Resources\\GitHub");

            foreach (var src in Directory.EnumerateFiles(dir))
            {
                string fileName = Path.GetFileName(src);
                string dest = Path.Combine(solutionRoot, fileName);

                if (!File.Exists(dest))
                {
                    var content = await ReplaceTokens(src, manifest);

                    File.WriteAllText(dest, content);

                    if (_visible.Contains(fileName))
                        AddFileToSolutionFolder(dest, (Solution2)DTE.Solution);
                }
            }
        }

        public bool UserWantToProceed()
        {
            string message = @"This will add some files to the solution folder and add some of them to Solution Items.

The files are:

  .gitattributes
  .gitignore
  CHANGELOG.md
  CONTRIBUTING.md
  LICENSE
  README.md

Files that already exist will not be overridden. Do you wish to continue?";

            return MessageBox.Show(message, Vsix.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public static void AddFileToSolutionFolder(string file, Solution2 solution)
        {
            Project currentProject = null;

            foreach (Project project in solution.Projects)
            {
                if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems && project.Name == "Solution Items")
                {
                    currentProject = project;
                    break;
                }
            }

            if (currentProject == null)
                currentProject = solution.AddSolutionFolder("Solution Items");

            currentProject.ProjectItems.AddFromFile(file);
        }

        public static async System.Threading.Tasks.Task<string> ReplaceTokens(string file, Manifest manifest, Dictionary<string, string> additionalInfo = null)
        {
            using (var reader = new StreamReader(file))
            {
                var content = await reader.ReadToEndAsync();

                if (manifest != null)
                {
                    var properties = typeof(Manifest).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (var property in properties)
                    {
                        var value = property.GetValue(manifest);

                        if (value != null)
                            content = content.Replace("{" + property.Name + "}", value.ToString());
                    }
                }

                if (additionalInfo != null)
                {
                    foreach (var keyValuePair in additionalInfo)
                    {
                        content = content.Replace("{" + keyValuePair.Key + "}", keyValuePair.Value);
                    }
                }

                return content;
            }
        }
    }
}
