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

namespace PrepareForGitHub
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]       
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidPrepForGitPackageString)]
    public sealed class PrepForGitPackage : Package
    {
        public PrepForGitPackage()
        {
        }

        protected override void Initialize()
        {
            PrepForGit.Initialize(this);
            base.Initialize();
        }

    }
}
