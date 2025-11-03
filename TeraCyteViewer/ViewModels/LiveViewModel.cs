using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TeraCyteViewer.Models;
using TeraCyteViewer.Services;
using TeraCyteViewer.Utils;

namespace TeraCyteViewer.ViewModels
{
    public partial class LiveViewModel : ObservableObject
    {
        // Live frame and inference fields bound to the view
        [ObservableProperty] private BitmapImage? currentImage;
        [ObservableProperty] private string? classificationLabel;
        [ObservableProperty] private double intensityAverage;
        [ObservableProperty] private double focusScore;
        [ObservableProperty] private string? imageId;
        [ObservableProperty] private DateTime lastUpdated;
        [ObservableProperty] private int[]? histogram;

        // Most-recent-first history for quick preview
        public ObservableCollection<ImageResultItem> History { get; } = new();

        // UI state flags and messaging
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isError;
        [ObservableProperty] private string statusMessage = "Ready";

        // "Stale" indicates data is temporarily unreliable 
        [ObservableProperty] private bool isStale;
        [ObservableProperty] private bool showOverlay;
        [ObservableProperty] private string overlayMessage = "";

        private readonly ApiClient _api;
        private readonly ILogger<LiveViewModel> _log;

        // Color feedback in the status bar based on current state
        public Brush StatusBrush
        {
            get
            {
                if (IsError)
                    return Brushes.IndianRed;         // error -> red
                if (IsStale)
                    return Brushes.Goldenrod;        // degraded -> yellow
                return Brushes.MediumSeaGreen;       // healthy -> green
            }
        }

        // Keep brush in sync when these flags flip
        partial void OnIsErrorChanged(bool value) => OnPropertyChanged(nameof(StatusBrush));
        partial void OnIsStaleChanged(bool value) => OnPropertyChanged(nameof(StatusBrush));

        public LiveViewModel(ApiClient api, ILogger<LiveViewModel> log)
        {
            _api = api;
            _log = log;
        }

        // Manual refresh entry-point for the UI (button)
        [RelayCommand]
        private async Task RefreshNowAsync()
        {
            if (IsBusy) return;
            IsBusy = true; IsError = false; StatusMessage = "Fetching data...";
            _log.LogInformation("Manual refresh started");

            try
            {
                // Short-circuit requests that hang
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

                // 1) Fetch latest image
                var img = await _api.GetImageAsync(cts.Token);

                ImageId = img.image_id;
                CurrentImage = ImageHelper.FromBase64PngSafe(img.image_data_base64, _log);

                // 2) Fetch results for that image
                var res = await _api.GetResultsAsync(cts.Token);

                // Guard against race where results are for a different frame
                if (res.image_id != img.image_id)
                {
                    _log.LogWarning("Mismatch: image_id {I1} vs results_id {I2}", img.image_id, res.image_id);
                    StatusMessage = "Waiting for matching results...";
                    return;
                }

                // Apply results to the UI
                ClassificationLabel = res.classification_label;
                IntensityAverage = res.intensity_average;
                FocusScore = res.focus_score;
                Histogram = res.histogram?.ToArray();

                LastUpdated = DateTime.Now;
                StatusMessage = "Updated successfully";
                AddToHistory(img, res, CurrentImage);
                _log.LogInformation("Manual refresh completed, image {Id}", img.image_id);
            }
            catch (Exception ex)
            {
                // Show concise message to the user; full detail goes to logs
                IsError = true;
                StatusMessage = "Error: " + ex.Message;
                _log.LogError(ex, "Refresh failed");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Inserts a compact record to the top of the history list, trimming overflow
        public void AddToHistory(ImageResponse img, ResultsResponse res, BitmapImage image)
        {
            if (image is null || string.IsNullOrEmpty(img.image_id))
                return;

            // Avoid duplicate consecutive entries
            if (History.Count > 0 && History[0].ImageId == img.image_id)
                return;

            // Prefer server timestamp when available; fall back to now
            DateTime ts = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(img.timestamp) &&
                DateTime.TryParse(img.timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsed))
            {
                ts = parsed.ToLocalTime();
            }

            var item = new ImageResultItem
            {
                ImageId = img.image_id,
                Image = image,
                Label = res.classification_label ?? "",
                Intensity = res.intensity_average,
                Focus = res.focus_score,
                Timestamp = ts,
                Histogram = res.histogram?.ToArray()
            };

            History.Insert(0, item);

            // Soft cap to keep memory and UI snappy
            const int maxItems = 100;
            if (History.Count > maxItems)
                History.RemoveAt(History.Count - 1);
        }
    }
}
