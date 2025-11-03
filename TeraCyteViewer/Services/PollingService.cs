using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TeraCyteViewer.ViewModels;
using TeraCyteViewer.Utils;

namespace TeraCyteViewer.Services
{
    public class PollingService : IDisposable
    {
        private readonly ApiClient _api;
        private readonly LiveViewModel _vm;
        private readonly ILogger<PollingService> _log;
        private readonly TimeSpan _interval;
        private  INavigationService? _nav;
        private readonly AuthService _auth;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private string? _lastImageId;

        public PollingService(ApiClient api, LiveViewModel vm, ILogger<PollingService> log,  AuthService auth, TimeSpan? interval = null)
        {
            _api = api;
            _vm = vm;
            _log = log;
            
            _auth = auth;
            _interval = interval ?? TimeSpan.FromSeconds(3);
        }
        public void SetNavigator(INavigationService nav) => _nav = nav;
        public void Start()
        {
            if (_loopTask != null && !_loopTask.IsCompleted) return;
            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => LoopAsync(_cts.Token));
            _log.LogInformation("Polling started with interval {Interval}ms", _interval.TotalMilliseconds);
        }

        private async Task LoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var img = await _api.GetImageAsync(token);

                    if (img.image_id != _lastImageId)
                    {
                        _log.LogInformation("New image detected: {Id}", img.image_id);
                        _lastImageId = img.image_id;

                        // Update ImageId immediately so the UI always shows the latest id
                        await App.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _vm.ImageId = img.image_id;
                            _vm.StatusMessage = "New image detected. Fetching results...";
                            _vm.IsError = false;

                        });

                        // Fetch results for this image
                        var res = await _api.GetResultsAsync(token);

                        if (res.image_id == img.image_id)
                        {
                            bool invalidData =
                                string.Equals(res.classification_label, "UNKNOWN_CLASSIFICATION", StringComparison.OrdinalIgnoreCase)
                                || res.intensity_average < 0
                                || res.focus_score < 0 || res.focus_score > 1;

                            if (invalidData)
                            {
                                _log.LogWarning("Invalid inference data for image {Id}: label={Label}, avg={Avg}, focus={Focus}",
                                    res.image_id, res.classification_label, res.intensity_average, res.focus_score);

                                await App.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    _vm.IsStale = true;
                                    _vm.OverlayMessage = "Data not valid (retrying...)";
                                    _vm.ShowOverlay = true;

                                    _vm.ClassificationLabel = "Unknown";
                                    _vm.IntensityAverage = double.NaN;
                                    _vm.FocusScore = double.NaN;
                                    _vm.StatusMessage = "Data not valid (retrying...)";
                                    _vm.IsError = true;
                                });

                                // Skip updating the image or histogram - keep last good frame
                                continue;
                            }

                            // Valid data - update normally
                            var bmp = ImageHelper.FromBase64PngSafe(img.image_data_base64, _log);
                            if (bmp != null)
                            {
                                await App.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    _vm.IsStale = false;
                                    _vm.ShowOverlay = false;

                                    _vm.ImageId = img.image_id;
                                    _vm.CurrentImage = bmp;
                                    _vm.ClassificationLabel = res.classification_label;
                                    _vm.IntensityAverage = res.intensity_average;
                                    _vm.FocusScore = res.focus_score;
                                    _vm.Histogram = res.histogram?.ToArray();
                                    _vm.LastUpdated = DateTime.Now;
                                    _vm.StatusMessage = "Image updated successfully";
                                    _vm.IsError = false;
                                    _vm.AddToHistory(img, res, bmp);
                                });
                            }
                            else
                            {
                                _log.LogWarning("Invalid image data for {Id}", img.image_id);
                                await App.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    _vm.IsStale = true;
                                    _vm.ShowOverlay = true;
                                    _vm.OverlayMessage = "Invalid image data (retrying...)";
                                    _vm.StatusMessage = "Invalid image data (retrying...)";
                                    _vm.IsError = true;
                                });
                            }
                        }

                        else
                        {
                            _log.LogWarning("Results out-of-sync: image {I1}, results {I2}", img.image_id, res.image_id);
                            await App.Current.Dispatcher.InvokeAsync(() =>
                            {
                                // Keep showing the new ImageId; inform the user we're waiting for matching results
                                _vm.StatusMessage = "Waiting for matching results...";
                                _vm.IsStale = true;
                                _vm.IsError = false;
                            });
                        }
                    }
                }
                catch (UnauthorizedAccessException uaex)
                {
                    _log.LogWarning(uaex, "Session expired. Navigating to login.");
                    Stop();

                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _vm.IsStale = true;
                        _vm.ShowOverlay = true;
                        _vm.OverlayMessage = "Session expired. Please log in again.";
                        _vm.StatusMessage = "Session expired.";
                        _vm.IsError = true;

                        // clear tokens 
                        _auth.GetType().GetProperty("AccessToken")?.SetValue(_auth, null);
                        _auth.GetType().GetProperty("RefreshToken")?.SetValue(_auth, null);

                        _nav?.ShowLogin();
                    });

                    return; // exit loop
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Polling iteration failed");
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _vm.IsStale = true;
                        _vm.IsError = true; 
                        _vm.ShowOverlay = true;
                        _vm.OverlayMessage = "Temporary issue – keeping last good frame";
                        _vm.StatusMessage = "Temporary issue (will retry)...";
                    });
                }

                try
                {
                    await Task.Delay(_interval, token);
                }
                catch
                {
                    // ignore cancellation delay exceptions
                }
            }
        }


        public void Stop() { _cts?.Cancel(); }
        public void Dispose() { Stop(); _cts?.Dispose(); }
    }
}
