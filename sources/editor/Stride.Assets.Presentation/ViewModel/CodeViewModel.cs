// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Stride.Assets.Presentation.AssetEditors;
using Stride.Assets.Presentation.AssetEditors.ScriptEditor;
using Stride.Assets.Scripts;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;
using RoslynWorkspace = Stride.Assets.Presentation.AssetEditors.ScriptEditor.RoslynWorkspace;

namespace Stride.Assets.Presentation.ViewModel
{
    /// <summary>
    /// Manages source code project and files, including change tracking, Roslyn workspace updates, etc...
    /// </summary>
    public class CodeViewModel : DispatcherViewModel, IDisposable
    {
        /// <summary>
        /// The editor minimum font size.
        /// </summary>
        public const int MinimumEditorFontSize = 8;

        /// <summary>
        /// The editor maximum font size.
        /// </summary>
        public const int MaximumEditorFontSize = 72;

        private readonly TaskCompletionSource<ProjectWatcher> projectWatcherCompletion = new();
        // private readonly TaskCompletionSource<RoslynWorkspace> workspaceCompletion = new();
        private int editorFontSize = ScriptEditorSettings.FontSize.GetValue(); // default size

        private readonly Brush keywordBrush;
        private readonly Brush typeBrush;

        public CodeViewModel(StrideAssetsViewModel strideAssetsViewModel)
            : base(strideAssetsViewModel.SafeArgument(nameof(strideAssetsViewModel)).ServiceProvider)
        {

        }

        /// <summary>
        /// Gets the project watcher which tracks source code changes on the disk; it is created asychronously.
        /// </summary>
        public Task<ProjectWatcher> ProjectWatcher => projectWatcherCompletion.Task;

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(CodeViewModel));
            Cleanup();
            base.Destroy();
        }

        /// <summary>
        /// // Handle Script asset deletion (from Visual Studio/HDD external changes to Game Studio).
        /// </summary>
        /// <returns>False if user refused to continue (in case deleted assets were dirty).</returns>
        private static async Task<bool> DeleteRemovedProjectAssets(ProjectViewModel projectViewModel, List<AssetViewModel> projectAssets, Project project, List<(UFile FilePath, UFile Link)> projectFiles)
        {
            // List IProjectAsset
            var currentProjectAssets = projectAssets.Where(x => x.AssetItem.Asset is IProjectAsset);

            var assetsToDelete = new List<AssetViewModel>();
            foreach (var asset in currentProjectAssets)
            {
                // Note: if file doesn't exist on HDD anymore (i.e. automatic csproj tracking for *.cs), no need to delete it anymore
                bool isDeleted = !projectFiles.Any(x => x.FilePath == asset.AssetItem.FullPath);
                if (isDeleted)
                {
                    assetsToDelete.Add(asset);
                }
            }

            var dirtyAssetsToDelete = assetsToDelete.Where(x => x.AssetItem.IsDirty).ToList();
            if (dirtyAssetsToDelete.Count > 0)
            {
                // Check if user is OK with deleting those dirty assets?
                var dialogResult = projectViewModel.Session.Dialogs.BlockingMessageBox(
                    string.Format(
                        Tr._p("Message", "The following source files in the {0} project have been deleted externally, but have unsaved changes in Game Studio. Do you want to delete these files?\r\n\r\n{1}"),
                       Path.GetFileName(project.FilePath), string.Join("\r\n", dirtyAssetsToDelete.Select(x => x.AssetItem.FullPath.ToWindowsPath()))),
                    MessageBoxButton.OKCancel);
                if (dialogResult == MessageBoxResult.Cancel)
                    return false;
            }

            // delete this asset
            if (assetsToDelete.Count > 0)
            {
                // TODO: this action (it should occur only during assembly releoad) will be undoable (undoing reload restores deleted script)
                if (!await projectViewModel.Session.ActiveAssetView.DeleteContent(assetsToDelete, true))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Handles project asset addition (from Visual Studio/HDD external changes to Game Studio).
        /// </summary>
        private static void AddNewProjectAssets(ProjectViewModel projectViewModel, List<AssetViewModel> projectAssets, Project project, List<(UFile FilePath, UFile Link)> projectFiles)
        {
            // Nothing to add?
            if (projectFiles.Count == 0)
                return;

            var scriptAssets = projectAssets.Where(x => x.AssetItem.Asset is IProjectAsset).Select(x => x.AssetItem);

            var documentsToIgnore = (from scriptAsset in scriptAssets
                                     from document in projectFiles
                                     let ufileDoc = document.FilePath
                                     where ufileDoc == scriptAsset.FullPath
                                     select document).ToList();

            //remove what we have already
            var documentsCopy = new List<(UFile FilePath, UFile Link)>(projectFiles);
            foreach (var document in documentsToIgnore)
            {
                documentsCopy.Remove(document);
            }

            //add what we are missing
            if (documentsCopy.Count > 0)
            {
                var newScriptAssets = new List<AssetViewModel>();
                foreach (var document in documentsCopy)
                {
                    var docFile = new UFile(document.FilePath);
                    var projFile = new UFile(project.FilePath);

                    var assetName = docFile.MakeRelative(projectViewModel.Package.RootDirectory).GetDirectoryAndFileNameWithoutExtension();

                    var asset = new ScriptSourceFileAsset();
                    var assetItem = new AssetItem(assetName, asset)
                    {
                        IsDirty = true, //todo review / this is actually very important in the case of renaming, to propagate the change from VS to Game Studio, if we set it false here, during renaming the renamed asset won't be removed
                        SourceFolder = projectViewModel.Package.RootDirectory,
                    };

                    var directory = projectViewModel.GetOrCreateProjectDirectory(assetItem.Location.GetFullDirectory().FullPath, false);
                    var newScriptAsset = projectViewModel.CreateAsset(directory, assetItem, false, null);
                    newScriptAssets.Add(newScriptAsset);
                }

                // We're out of any transaction in this context so we have to manually notify that new assets were created.
                projectViewModel.Session.NotifyAssetPropertiesChanged(newScriptAssets);
            }
        }

        /// <summary>
        /// Enumerate assets.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="list"></param>
        /// <returns>Returns true if any of the (sub)directories is dirty.</returns>
        private static bool GetAssets(DirectoryBaseViewModel dir, List<AssetViewModel> list)
        {
            bool dirDirty = dir.IsDirty;

            foreach (var directory in dir.SubDirectories)
            {
                dirDirty |= GetAssets(directory, list);
            }

            //is dirty check is necessary to avoid unsaved scripts to be deleted
            list.AddRange(dir.Assets.Where(x => x.Asset is IProjectAsset));

            return dirDirty;
        }

        private void UpdateDirtiness(DirectoryBaseViewModel dir, bool isDirty)
        {
            if (dir.IsDirty)
                ((IDirtiable)dir).UpdateDirtiness(isDirty);

            foreach (var subdir in dir.SubDirectories)
            {
                UpdateDirtiness(subdir, isDirty);
            }
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            // nothing for now
        }

        private void StyleRunFromSymbolDisplayPartKind(SymbolDisplayPartKind partKind, Run run)
        {
            switch (partKind)
            {
                case SymbolDisplayPartKind.Keyword:
                    run.Foreground = keywordBrush;
                    return;
                case SymbolDisplayPartKind.StructName:
                case SymbolDisplayPartKind.EnumName:
                case SymbolDisplayPartKind.TypeParameterName:
                case SymbolDisplayPartKind.ClassName:
                case SymbolDisplayPartKind.DelegateName:
                case SymbolDisplayPartKind.InterfaceName:
                    run.Foreground = typeBrush;
                    return;
            }
        }

        private void StyleRunFromTextTag(string textTag, Run run)
        {
            switch (textTag)
            {
                case TextTags.Keyword:
                    run.Foreground = keywordBrush;
                    break;
                case TextTags.Struct:
                case TextTags.Enum:
                case TextTags.TypeParameter:
                case TextTags.Class:
                case TextTags.Delegate:
                case TextTags.Interface:
                    run.Foreground = typeBrush;
                    break;
            }
        }
    }
}
