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
        [ObservableProperty] private BitmapImage? currentImage;
        [ObservableProperty] private string? classificationLabel;
        [ObservableProperty] private double intensityAverage;
        [ObservableProperty] private double focusScore;
        [ObservableProperty] private string? imageId;
        [ObservableProperty] private DateTime lastUpdated;
        [ObservableProperty] private int[]? histogram;

        public ObservableCollection<ImageResultItem> History { get; } = new();


        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isError;
        [ObservableProperty] private string statusMessage = "Ready";

        [ObservableProperty] private bool isStale;             
        [ObservableProperty] private bool showOverlay;         
        [ObservableProperty] private string overlayMessage = "";

        private readonly ApiClient _api;
        private readonly ILogger<LiveViewModel> _log;

        public Brush StatusBrush
        {
            get
            {
                if (IsError)
                    return Brushes.IndianRed;         // Red
                if (IsStale)
                    return Brushes.Goldenrod;        // Yellow
                return Brushes.MediumSeaGreen;       // Green
            }
        }

        partial void OnIsErrorChanged(bool value) => OnPropertyChanged(nameof(StatusBrush));
        partial void OnIsStaleChanged(bool value) => OnPropertyChanged(nameof(StatusBrush));


        public LiveViewModel(ApiClient api, ILogger<LiveViewModel> log)
        {
            _api = api;
            _log = log;
        }

        [RelayCommand]
        private async Task RefreshNowAsync()
        {
            if (IsBusy) return;
            IsBusy = true; IsError = false; StatusMessage = "Fetching data...";
            _log.LogInformation("Manual refresh started");

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var img = await _api.GetImageAsync(cts.Token);

                ImageId = img.image_id;
                CurrentImage = ImageHelper.FromBase64PngSafe(img.image_data_base64, _log);
                var res = await _api.GetResultsAsync(cts.Token);

                if (res.image_id != img.image_id)
                {
                    _log.LogWarning("Mismatch: image_id {I1} vs results_id {I2}", img.image_id, res.image_id);
                    StatusMessage = "Waiting for matching results...";
                    return;
                }

                ClassificationLabel = res.classification_label;
                IntensityAverage = res.intensity_average;
                FocusScore = res.focus_score;
                Histogram = res.histogram?.ToArray();

                LastUpdated = DateTime.Now;
                StatusMessage = "Updated successfully";
                AddToHistory(img, res,CurrentImage);
                _log.LogInformation("Manual refresh completed, image {Id}", img.image_id);
            }
            catch (Exception ex)
            {
                IsError = true;
                StatusMessage = "Error: " + ex.Message;
                _log.LogError(ex, "Refresh failed");
            }
            finally
            {
                IsBusy = false;
            }
        }
        public void AddToHistory(ImageResponse img, ResultsResponse res, BitmapImage image)
        {
            if (image is null || string.IsNullOrEmpty(img.image_id))
                return;

            if (History.Count > 0 && History[0].ImageId == img.image_id)
                return;

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

            const int maxItems = 100;
            if (History.Count > maxItems)
                History.RemoveAt(History.Count - 1);
        }
    }
}
