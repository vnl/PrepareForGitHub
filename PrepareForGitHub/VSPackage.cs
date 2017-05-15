using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using PrepareForGitHub.Shared.Helpers;

namespace PrepareForGitHub
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]       
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidPrepForGitPackageString)]
    public sealed class VSPackage : Package
    {
        public VSPackage()
        {
        }
        protected override void Initialize()
        {

            Logger.Initialize(this, Vsix.Name);
            ProjectHelpers.Initialize(this);

            PrepForGit.Initialize(this);
            base.Initialize();
        }

    }
}
