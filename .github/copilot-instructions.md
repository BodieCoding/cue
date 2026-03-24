<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# Cue - Custom Copilot Instructions

## Project Overview
Cue is a lightweight C# WPF desktop application featuring an animated face puppet that listens for local HTTP commands to update visual state and content.

## Key Principles
- **Visual Receiver Only**: This application is strictly a visual receiver. DO NOT generate LLM API wrappers, Anthropic SDK code, or conversation history logic.
- **Local HTTP Listener**: All communication happens through HTTP POST requests to `http://localhost:8081/update`.
- **Transparent UI**: The application uses a transparent, always-on-top WPF window with WebView2 for rendering.
- **Safety First**: Always dispatch UI updates to the main thread using `Dispatcher.Invoke()`.

## Important Constraints
1. No external LLM integrations
2. No API key management
3. No conversation persistence
4. No background processing or inference
5. No networking beyond the local HTTP listener

## Architecture
- **Frontend**: HTML/CSS/JavaScript in WebView2
- **Backend**: C# WPF application with HttpListener
- **Communication**: JSON POST requests with three fields:
  - `emotion_state`: "idle", "thinking", "speaking", or "alert"
  - `dialogue`: Text for speech bubble
  - `html_payload`: Custom HTML content

## Common Tasks
- **Adding Expressions**: Add new CSS classes to `.face` in MainWindow.xaml.cs HTML content
- **Updating Styles**: Modify the `<style>` block in the embedded HTML
- **Extending Animations**: Add @keyframes to the CSS for new animation effects
- **Modifying Window Properties**: Update MainWindow.xaml (AllowsTransparency, Topmost, Width, Height, etc.)

## Testing the Application
Use curl or any HTTP client to test:
```powershell
Invoke-WebRequest -Uri "http://localhost:8081/update" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"emotion_state":"speaking","dialogue":"Hello!","html_payload":null}'
```

## Code Organization
- `MainWindow.xaml.cs`: HTTP listener, WebView2 management, UI dispatch
- Embedded HTML: Face rendering, animations, and JavaScript update handler
- `App.xaml.cs`: Application initialization (minimal)

## Performance Considerations
- HTTP listener runs on background thread
- Large HTML payloads may impact animation smoothness
- WebView2 script execution is asynchronous
- Consider throttling rapid update requests
