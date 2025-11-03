# 🧬 TeraCyte Viewer

**A real-time WPF application for streaming microscope images and AI inference results from the TeraCyte backend.**  
Built with **.NET 8**, **MVVM**, and **WPF**, featuring JWT authentication, automatic token refresh, live polling, and a modern responsive UI.

---

## 🚀 Overview

TeraCyte Viewer connects to the [TeraCyte assignment API](https://assignment-server-rv-866595813231.us-central1.run.app/) to:

- Authenticate users via JWT tokens
- Poll protected endpoints for real-time microscope images and inference results
- Display live image data, metrics, and histograms
- Handle token refreshes, stale data, and API failures gracefully
- Maintain a scrollable history of previously fetched images
- Provide smooth animations and a clean, production-ready UX

---

## 🧩 Architecture at a Glance

TeraCyteViewer
│
├── Models/
│ ├── ImageResponse.cs // /api/image DTO
│ ├── ResultsResponse.cs // /api/results DTO
│ └── ImageResultItem.cs // History item (image + metrics)
│
├── Services/
│ ├── AuthService.cs // JWT login, refresh, expiry logic
│ ├── ApiClient.cs // Fetches images & results with auto retry
│ ├── PollingService.cs // Periodic data fetching and stale handling
│ ├── NavigationService.cs // Switches between views (Login ↔ Live)
│
├── ViewModels/
│ ├── LoginViewModel.cs // Authentication logic and commands
│ ├── LiveViewModel.cs // Main logic for image/results/history
│ ├── MainViewModel.cs // High-level app state
│
├── Views/
│ ├── LoginView.xaml // Login UI
│ ├── LiveView.xaml // Main real-time view
│ ├── HistogramView.xaml // ScottPlot histogram component
│ ├── HistoryPreviewWindow.xaml // Popup preview for past images
│
├── Utils/
│ ├── ImageHelper.cs // Base64 → BitmapImage converter
│ ├── BoolToVisibilityConverter.cs
│ ├── BoolToOpacityConverter.cs
│
└── App.xaml / App.xaml.cs // DI setup, logging, exception handling

---

## 🧠 Key Features

| Category          | Description                                                                       |
| ----------------- | --------------------------------------------------------------------------------- |
| 🔑 Authentication | Login with username/password, manage JWT, auto-refresh tokens before expiry       |
| 🔄 Token Refresh  | On `401 Unauthorized`, refresh tokens and retry once before logout                |
| 🕑 Polling        | Background polling of `/api/image` and `/api/results` every few seconds           |
| 🧩 Data Pairing   | Only update when a **new image_id** appears, ensuring matched image/result pairs  |
| 📊 Visualization  | Live histogram rendered with **ScottPlot.WPF** (256 bins, intensity counts)       |
| 🧱 State Handling | Graceful handling of stale data, API timeouts, unknown classifications            |
| 🗂 History         | Scrollable history panel with clickable previews of previous results              |
| ✨ UI/UX          | Animated fade & zoom-in transitions, color-coded status, overlay messages         |
| 🧾 Logging        | Detailed logging via **Serilog** (Auth, API, refresh, polling, errors)            |
| 💥 Error Recovery | Automatic retries for transient 5xx errors, logout on unrecoverable auth failures |

---

## 🧰 Tech Stack

- **.NET 8 / WPF**
- **MVVM Toolkit** (`CommunityToolkit.Mvvm`)
- **Serilog** for logging
- **ScottPlot.WPF** for chart visualization
- **Dependency Injection** via `Microsoft.Extensions.Hosting`
- **Async/await + CancellationTokens** for robust async flow

---

## ⚙️ Setup & Run

### Prerequisites

- Windows 10+
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code

### Clone & Restore

```bash
git clone https://github.com/yourusername/TeraCyteViewer.git
cd TeraCyteViewer
dotnet restore
```

### Run

```
dotnet run --project TeraCyteViewer
```

Or directly from Visual Studio (F5)
