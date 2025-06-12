using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using BiliBiliMusic.Extensions;
using BiliBiliMusic.Helpers;
using BiliBiliMusic.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BiliBiliMusic.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] public partial string ResolveUrl { get; set; } = "";
    [ObservableProperty] public partial string StoreFolderPath { get; set; } = "";
    [ObservableProperty] public partial string FileName { get; set; } = "";
    [ObservableProperty] public partial bool IsAutoFileName { get; set; } = true;
    [ObservableProperty] public partial string ResolveStatusDescription { get; set; } = "Fields seem invalid, please check.";
    [ObservableProperty] public partial ResolveStatus ResolveStatus { get; set; } = ResolveStatus.Validating;

    #region Validation
    
    [ObservableProperty] public partial bool IsAllValid { get; set; }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if(e.PropertyName is nameof(ResolveUrl) or nameof(StoreFolderPath) or nameof(FileName) or nameof(IsAutoFileName))
        {
            IsAllValid = false;
            ResolveStatus = ResolveStatus.Validating;
            ResolveStatusDescription = "Fields seem invalid, please check.";
            // Resolve URL
            if (string.IsNullOrEmpty(ResolveUrl))
            {
                ResolveStatus = ResolveStatus.Validating;
                ResolveStatusDescription = "URL cannot be empty.";
            }
            else if (!ResolveUrl.StartsWith("https://www.bilibili.com/video/"))
            {
                ResolveStatus = ResolveStatus.Validating;
                ResolveStatusDescription = "Invalid BiliBili video URL: must start with 'https://www.bilibili.com/video/'.";
            }
            // Store Folder Path
            else if (string.IsNullOrEmpty(StoreFolderPath))
            {
                ResolveStatus = ResolveStatus.Validating;
                ResolveStatusDescription = "Folder Path cannot be empty.";
            }
            else if (!Path.Exists(StoreFolderPath))
            {
                ResolveStatus = ResolveStatus.Validating;
                ResolveStatusDescription = "Folder Path not found.";
            }
            // File Name
            else if (!IsAutoFileName && string.IsNullOrEmpty(FileName))
            {
                ResolveStatus = ResolveStatus.Validating;
                ResolveStatusDescription = "File Name cannot be empty when Auto File Name is disabled.";
            }
            else if (!IsAutoFileName && FileName.GetValidFileName() == null)
            {
                ResolveStatus = ResolveStatus.Validating;
                ResolveStatusDescription = "Invalid File Name.";
            }
            else
            {
                IsAllValid = true;
                ResolveStatus = ResolveStatus.WaitForResolving;
                ResolveStatusDescription = "Ready to resolve.";
            }
        }
        base.OnPropertyChanged(e);
    }

    #endregion

    [RelayCommand]
    private async Task Browse()
    {
        var folderPath = await App.MainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false
        });
        if (folderPath.Count == 1)
        {
            StoreFolderPath = folderPath[0].TryGetLocalPath() ?? "";
        }
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

            var response = await BiliBiliHelper.TestConnection(audioUrl);
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
            await BiliBiliHelper.DownloadAudioAsync(audioUrl!, Path.Combine(StoreFolderPath, fileNameWithExtension), progress, CancellationToken.None);
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