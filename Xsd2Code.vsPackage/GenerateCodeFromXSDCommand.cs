//------------------------------------------------------------------------------
// <copyright file="GenerateCodeFromXSDCommand.cs" company="Microsoft">
//     Copyright (c) Microsoft.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Xsd2Code.ConfigurationForm;
using Xsd2Code.Library;
using Xsd2Code.Library.Helpers;
using Task = System.Threading.Tasks.Task;

namespace Xsd2Code.vsPackage
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GenerateCodeFromXSDCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("b28b1de2-6cf0-4ff6-8a20-0123954dc58c");

        public AsyncPackage Package { get; }

        public DTE Dte { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateCodeFromXSDCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private GenerateCodeFromXSDCommand(AsyncPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.Package = package;

           
        }

        private async Task RegisterCommand()
        {
            IMenuCommandService commandService = await this.Package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(this.MenuItemCallback, menuCommandID); // using OleMenuCommand to have access to BeforeQueryStatus
                menuItem.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
                // Switch to Main Thread before calling AddCommand which calls GetService() which should
                // always be called on UI thread.
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                commandService.AddCommand(menuItem);
            }
            Dte = (DTE) await Package.GetServiceAsync(typeof(DTE));
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GenerateCodeFromXSDCommand Instance
        {
            get;
            private set;
        }

        // Asynchronous initialization
        public static async Task InitializeAsync(AsyncPackage package)
        {
            Instance = new GenerateCodeFromXSDCommand(package);
            await Instance.RegisterCommand();
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            openConfigurationWindow();
        }

        /// <summary>
        /// Code from Xsd2Code.Addin::Connect
        /// </summary>
        private void openConfigurationWindow()
        {
           
            ProjectItem proitem = Dte.SelectedItems.Item(1).ProjectItem;
            Project proj = proitem.ContainingProject;
            string projectDirectory = Path.GetDirectoryName(proj.FullName);

            // Try to get default nameSpace
            string defaultNamespace = string.Empty;
            uint? targetFramework = 0;
            bool? isSilverlightApp = false;
            try
            {
                defaultNamespace = proj.Properties.Item("DefaultNamespace").Value as string;
                targetFramework = proj.Properties.Item("TargetFramework").Value as uint?;
                isSilverlightApp = proj.Properties.Item("SilverlightProject.IsSilverlightApplication").Value as bool?;
            }
            catch
            {
            }

            string xsdFileName = proitem.FileNames[0];
            try
            {
                proitem.Save(xsdFileName);
            }
            catch (Exception)
            {
            }

            TargetFramework framework = TargetFramework.Net20;
            if (targetFramework.HasValue)
            {
                uint target = targetFramework.Value;
                switch (target)
                {
                    case 196608:
                        framework = TargetFramework.Net30;
                        break;
                    case 196613:
                        framework = TargetFramework.Net35;
                        break;
                    case 262144:
                        framework = TargetFramework.Net40;
                        break;
                }
            }
            if (isSilverlightApp.HasValue)
            {
                if (isSilverlightApp.Value)
                {
                    framework = TargetFramework.Silverlight;
                }
            }

            // We associate an outputfile with the selected XSD file to know were to look for the parameters
            // TODO embed all the parameters as attributes of the XSD file in the project ?
            IVsHierarchy hierarchy = null;
            uint itemid;
            string outputFile = null;
            IVsBuildPropertyStorage buildPropertyStorage = null;
            if (IsSingleProjectItemSelection(out hierarchy, out itemid))
            {
                buildPropertyStorage = hierarchy as IVsBuildPropertyStorage;
                if (buildPropertyStorage != null)
                {
                    buildPropertyStorage.GetItemAttribute(itemid, "Xsd2CodeOutputFile", out outputFile);
                }
            }

            var frm = new FormOption();
            frm.Init(xsdFileName, proj.CodeModel.Language, defaultNamespace, framework, outputFile);

            DialogResult result = frm.ShowDialog();

            GeneratorParams generatorParams = frm.GeneratorParams.Clone();
            generatorParams.InputFilePath = xsdFileName;

            var gen = new GeneratorFacade(generatorParams);

            bool foundOutputFile = false;

            if (xsdFileName.Length > 0)
            {
                if (result == DialogResult.OK)
                {
                    // Close file if open in IDE
                    ProjectItem projElmts = null;
                    if (!String.IsNullOrEmpty(outputFile))
                    {
                        string rootedOutputFile = Path.IsPathRooted(outputFile) ? outputFile : Path.Combine(projectDirectory, outputFile);
                        foundOutputFile = FindInProject(proj.ProjectItems, rootedOutputFile, out projElmts);
                        if (foundOutputFile)
                        {
                            Window window = projElmts.Open(EnvDTE.Constants.vsViewKindCode);
                            window.Close(vsSaveChanges.vsSaveChangesNo);
                        }
                    }

                    Result<List<string>> generateResult = gen.Generate();
                    List<string> outputFileNames = generateResult.Entity;

                    if (!generateResult.Success)
                        MessageBox.Show(generateResult.Messages.ToString(), "XSD2Code", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                    {
                        // Save one of the output file path so we can reada the parameters from it the next time
                        if (buildPropertyStorage != null)
                        {
                            buildPropertyStorage.SetItemAttribute(itemid, "Xsd2CodeOutputFile", outputFileNames[0]);
                        }

                        // try again now that the generation occured
                        string newRootedOutputFile = Path.Combine(projectDirectory, outputFileNames[0]);
                        foundOutputFile = FindInProject(proj.ProjectItems, newRootedOutputFile, out projElmts);
                        if (!foundOutputFile)
                        {
                            projElmts = proj.ProjectItems.AddFromFile(newRootedOutputFile);
                        }


                        if (frm.OpenAfterGeneration && projElmts != null)
                        {
                            Window window = projElmts.Open(EnvDTE.Constants.vsViewKindCode);
                            window.Activate();
                            window.SetFocus();

                            try
                            {
                                // this.applicationObjectField.DTE.ExecuteCommand("Edit.RemoveAndSort", "");
                                Dte.ExecuteCommand("Edit.FormatDocument", string.Empty);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }

            return;

        }

        /// <summary>
        /// Checking the file extension. 
        /// Code from : http://www.diaryofaninja.com/blog/2014/02/18/who-said-building-visual-studio-extensions-was-hard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void menuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            // get the menu that fired the event
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                // start by assuming that the menu will not be shown
                menuCommand.Visible = false;
                menuCommand.Enabled = false;

                IVsHierarchy hierarchy = null;
                uint itemid = VSConstants.VSITEMID_NIL;

                if (!IsSingleProjectItemSelection(out hierarchy, out itemid)) return;
                // Get the file path
                string itemFullPath = null;
                ((IVsProject)hierarchy).GetMkDocument(itemid, out itemFullPath);
                var transformFileInfo = new FileInfo(itemFullPath);

                // then check if the file is named '*.xsd'
                bool isWebConfig = string.Compare(".xsd", transformFileInfo.Extension, StringComparison.OrdinalIgnoreCase) == 0;

                // if not leave the menu hidden
                if (!isWebConfig) return;

                menuCommand.Visible = true;
                menuCommand.Enabled = true;
            }
        }
        public static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
        {
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;
            int hr = VSConstants.S_OK;

            var monitorSelection = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return false;
            }

            IVsMultiItemSelect multiItemSelect = null;
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;

            try
            {
                hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    // there is no selection
                    return false;
                }

                // multiple items are selected
                if (multiItemSelect != null) return false;

                // there is a hierarchy root node selected, thus it is not a single item inside a project

                if (itemid == VSConstants.VSITEMID_ROOT) return false;

                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null) return false;

                Guid guidProjectID = Guid.Empty;

                if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectID)))
                {
                    return false; // hierarchy is not a project inside the Solution if it does not have a ProjectID Guid
                }

                // if we got this far then there is a single project item selected
                return true;
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }

        /// <summary>
        /// Recursive search in project solution.
        /// </summary>
        /// <param name="projectItems">
        /// Root projectItems
        /// </param>
        /// <param name="filename">
        /// Full path of search element
        /// </param>
        /// <param name="item">
        /// output ProjectItem
        /// </param>
        /// <returns>
        /// true if found else false
        /// </returns>
        private bool FindInProject(ProjectItems projectItems, string filename, out ProjectItem item)
        {
            item = null;
            if (projectItems == null)
                return false;

            foreach (ProjectItem projElmts in projectItems)
            {
                if (projElmts.get_FileNames(0) == filename)
                {
                    item = projElmts;
                    return true;
                }

                if (FindInProject(projElmts.ProjectItems, filename, out item))
                    return true;
            }

            return false;
        }
    }
}
