using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using BiliBiliMusic.Core.Extensions;
using BiliBiliMusic.Core.Helpers;
using BiliBiliMusic.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BiliBiliMusic.ViewModels;

public partial class MainWindowViewModel(IStorageProvider storageProvider) : ObservableObject
{
    [ObservableProperty] public partial string Title { get; set; } = $"BiliBili Music {AssemblyHelper.GetAssemblyVersion()}";
    [ObservableProperty] public partial string ResolveUrl { get; set; } = "";
    [ObservableProperty] public partial string StoreFolderPath { get; set; } = "";
    [ObservableProperty] public partial string FileName { get; set; } = "";
    [ObservableProperty] public partial bool IsAutoFileName { get; set; } = true;
    [ObservableProperty] public partial string ResolveStatusDescription { get; set; } = "Fields seem invalid, please check.";
    [ObservableProperty] public partial ResolveStatus ResolveStatus { get; set; } = ResolveStatus.Validating;
    
    private void StatusUpdate(ResolveStatus newStatus, string newDescription)
    {
        ResolveStatus = newStatus;
        ResolveStatusDescription = newDescription;
    }

    #region Validation
    
    [ObservableProperty] public partial bool IsAllValid { get; set; }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if(e.PropertyName is nameof(ResolveUrl) or nameof(StoreFolderPath) or nameof(FileName) or nameof(IsAutoFileName))
        {
            IsAllValid = false;
            StatusUpdate(ResolveStatus.Validating, "Fields seem invalid, please check.");
            
            // Resolve URL
            if (string.IsNullOrEmpty(ResolveUrl))
                StatusUpdate(ResolveStatus.Validating, "URL cannot be empty.");
            else if (!ResolveUrl.StartsWith("https://www.bilibili.com/video/"))
                StatusUpdate(ResolveStatus.Validating, "Invalid BiliBili video URL: must start with 'https://www.bilibili.com/video/'.");

            // Store Folder Path
            else if (string.IsNullOrEmpty(StoreFolderPath))
                StatusUpdate(ResolveStatus.Validating, "Folder Path cannot be empty.");
            else if (!Path.Exists(StoreFolderPath))
                StatusUpdate(ResolveStatus.Validating, "Folder Path not found.");
            
            // File Name
            else if (!IsAutoFileName && string.IsNullOrEmpty(FileName))
                StatusUpdate(ResolveStatus.Validating, "File Name cannot be empty when Auto File Name is disabled.");
            else if (!IsAutoFileName && FileName.GetValidFileName() == null)
                StatusUpdate(ResolveStatus.Validating, "Invalid File Name.");
            else
            {
                IsAllValid = true;
                StatusUpdate(ResolveStatus.WaitForResolving, "Ready to resolve.");
            }
        }
        base.OnPropertyChanged(e);
    }

    #endregion

    [RelayCommand]
    private async Task Browse()
    {
        var folderPath = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false
        });
        if (folderPath.Count == 1)
            StoreFolderPath = folderPath[0].TryGetLocalPath() ?? "";
    }

    [RelayCommand]
    private async Task Download()
    {
        var fileName = "";
        if (!IsAutoFileName)
            fileName = FileName;
        
        // Resolve
        var videoId = BiliBiliHelper.ExtractVideoId(ResolveUrl);
        ResolveStatus = ResolveStatus.Resolving;
        
        //// Resolve - Request Video Page
        var pageContent = "";
        try
        {
            ResolveStatusDescription = $"Requesting {videoId}";
            pageContent = await BiliBiliHelper.ResolveUrl(ResolveUrl);
        }
        catch (Exception e)
        {
            ResolveStatus = ResolveStatus.ResolveFailed;
            ResolveStatusDescription = $"Request failed: {e.Message}";
        }
        
        //// Resolve - Video Title
        if (IsAutoFileName)
        {
            try
            {
                fileName = BiliBiliHelper.ExtractVideoTitle(pageContent) ?? videoId;
            }
            catch (Exception e)
            {
                ResolveStatus = ResolveStatus.ResolveFailed;
                ResolveStatusDescription = $"Extract Video Title failed: {e.Message}";
            }
        }

        //// Resolve - Extract Audio URL
        var audioUrl = "";
        try
        {
            ResolveStatusDescription = "Extracting Audio URL";
            audioUrl = BiliBiliHelper.ExtractAudioUrl(pageContent);
        }
        catch (Exception e)
        {
            ResolveStatus = ResolveStatus.ResolveFailed;
            ResolveStatusDescription = $"Extract Audio URL failed: {e.Message}";
        }

        //// Resolve - Finalize
        try
        {
            if(audioUrl == null)
            {
                ResolveStatus = ResolveStatus.ResolveFailed;
                ResolveStatusDescription = "Audio URL not found.";
                return;
            }
            
            var response = await BiliBiliHelper.TestConnection(audioUrl, videoId);
            if (!response)
            {
                ResolveStatus = ResolveStatus.ResolveFailed;
                ResolveStatusDescription = "Audio URL is not accessible.";
                return;
            }
        }
        catch (Exception e)
        {
            ResolveStatus = ResolveStatus.ResolveFailed;
            ResolveStatusDescription = $"Audio URL testing failed: {e.Message}";
            return;
        }
        
        // Download
        ResolveStatus = ResolveStatus.Downloading;
        try
        {
            ResolveStatusDescription = "Downloading audio file...";
            var fileNameWithExtension = $"{fileName}.m4s";
            var progress = new Progress<HttpClientExtensions.HttpDownloadProgress>();
            progress.ProgressChanged += (_, downloadProgress) =>
            {
                ResolveStatusDescription = $"Downloading: {downloadProgress.BytesReceived} / {downloadProgress.TotalBytesToReceive} bytes";
            };
            await BiliBiliHelper.DownloadAudioAsync(audioUrl, videoId, Path.Combine(StoreFolderPath, fileNameWithExtension), progress, CancellationToken.None);
            ResolveStatus = ResolveStatus.Downloaded;
            ResolveStatusDescription = $"Download completed: {fileName}";
        }
        catch (Exception e)
        {
            ResolveStatus = ResolveStatus.DownloadFailed;
            ResolveStatusDescription = $"Download failed: {e.Message}";
        }
    }
}