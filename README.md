# Cue - Desktop UI Overlay Puppet

A lightweight C# WPF desktop application that displays an animated face puppet with dynamic expressions and content rendering. The application listens for local HTTP commands to update the puppet's emotional state, dialogue, and rich content display.

## Features

- **Transparent, Always-On-Top Window**: Borderless WPF window with transparency for seamless desktop integration
- **WebView2 Integration**: HTML/CSS/JavaScript rendering engine for rich visual content
- **HTTP Local Listener**: Listens on `http://localhost:8081/update` for POST requests
- **Dynamic Expressions**: Four emotion states - idle, thinking, speaking, and alert
- **Interactive Puppet**: Animated face with eyes and mouth that responds to emotion changes
- **Speech Bubble**: Displays dialogue text with smooth fade-in/fade-out transitions
- **Rich Content Area**: Injects custom HTML content for extended functionality

## Project Structure

```
Cue/
├── Cue.csproj              # Project file
├── Program.cs              # Entry point
├── App.xaml                # Application definition
├── App.xaml.cs             # Application code-behind
├── MainWindow.xaml         # WPF window definition
├── MainWindow.xaml.cs      # Window logic and HTTP listener
└── README.md              # This file
```

## Architecture

### C# Components

1. **MainWindow.xaml.cs** - Handles:
   - WebView2 initialization with transparent background
   - HTTP listener setup on port 8081
   - JSON parsing and validation
   - Safe UI thread dispatching
   - Script execution in WebView2

2. **HTTP Request Handler**:
   - Listens for POST requests at `http://localhost:8081/update`
   - Accepts JSON with three fields:
     - `emotion_state`: "idle", "thinking", "speaking", or "alert"
     - `dialogue`: Text to display in speech bubble (or null to hide)
     - `html_payload`: Custom HTML content (or null to hide)

### HTML/JavaScript Components

1. **Face Puppet** - CSS-styled circular element with:
   - Animated eyes with pupils
   - Animated mouth that responds to emotion
   - Four emotion states with distinct color gradients
   - Smooth animations and transitions

2. **Speech Bubble** - Text display element with:
   - Fade-in/fade-out transitions
   - Arrow pointer toward face
   - Scrollable content area

3. **Rich Content Area** - HTML injection point with:
   - Support for custom HTML, images, and links
   - Scrollable overflow handling
   - Styled with semi-transparent background

## API Usage

Send a POST request to `http://localhost:8081/update` with JSON payload:

```json
{
  "emotion_state": "speaking",
  "dialogue": "Hello! How can I help?",
  "html_payload": "<p>This is some <b>rich content</b></p>"
}
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `emotion_state` | string | Emotion state: "idle", "thinking", "speaking", "alert" |
| `dialogue` | string \| null | Text to display in speech bubble, or null to hide |
| `html_payload` | string \| null | HTML content to inject, or null to hide |

## Requirements

- .NET 6.0 or later
- Windows 10 or later
- WebView2 Runtime (automatically managed by NuGet package)

## Installation

1. Clone or download the project
2. Open in Visual Studio or Visual Studio Code
3. Restore NuGet packages: `dotnet restore`
4. Build the project: `dotnet build`

## Running the Application

```bash
dotnet run
```

The application will:
1. Launch a transparent, borderless window (400x600px)
2. Display an animated puppet in idle state
3. Start listening on `http://localhost:8081/update`
4. Ready to accept command requests

## Development Notes

- The window is positioned at (100, 100) by default; modify in MainWindow.xaml
- The HTTP listener runs on a background thread to prevent UI blocking
- All UI updates are safely dispatched to the main thread
- The WebView2 content is generated as an embedded HTML string
- JSON parsing includes validation to prevent malformed requests

## Security Considerations

This application is designed as a local-only service. The HTTP listener only accepts JSON and includes basic validation. For production use, consider:
- Adding authentication/authorization
- Rate limiting
- Input sanitization for HTML payloads
- Error logging and monitoring

## License

Cue is provided as-is for personal use.
