using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace Cue
{
    public partial class MainWindow : Window
    {
        private HttpListener? _httpListener;
        private CancellationTokenSource? _cancellationTokenSource;
        private Thread? _listenerThread;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize WebView2
            await InitializeWebView();

            // Start the HTTP listener
            StartHttpListener();
        }

private async Task InitializeWebView()
        {
            try
            {
                // Wait for WebView2 to initialize
                await WebView.EnsureCoreWebView2Async();

                // Force WebView2's base layer to be transparent
                WebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

                // ADD THIS LINE: Listen for messages from the JavaScript frontend
                WebView.WebMessageReceived += WebView_WebMessageReceived;

                // Load the HTML content
                string htmlContent = GetHtmlContent();
                WebView.CoreWebView2.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing WebView2: {ex.Message}");
            }
        }

// FIXED: Added the '?' to 'object? sender' to satisfy the strict nullability warning
        private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // FIXED: The correct WebView2 method is TryGetWebMessageAsString(), not TryGetAsString()
            if (e.TryGetWebMessageAsString() == "drag_window")
            {
                // UI updates must happen on the main dispatcher thread
                Dispatcher.Invoke(() =>
                {
                    // Check if the left mouse button is actually pressed down to avoid crashes
                    if (System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                    {
                        this.DragMove(); // This triggers native Windows dragging!
                    }
                });
            }
        }

        private void StartHttpListener()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _listenerThread = new Thread(() => ListenForUpdates(_cancellationTokenSource.Token))
            {
                IsBackground = true
            };
            _listenerThread.Start();
        }

 private void ListenForUpdates(CancellationToken cancellationToken)
{
    _httpListener = new HttpListener();
    // Listen on both to avoid IPv4/IPv6 loopback mismatches
    _httpListener.Prefixes.Add("http://127.0.0.1:8081/");
    _httpListener.Prefixes.Add("http://localhost:8081/");

    try
    {
        _httpListener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            // This blocks efficiently and waits for a real request.
            // It will throw an expected HttpListenerException when the app closes.
            HttpListenerContext context = _httpListener.GetContext();

            // Hand the request off to a background worker so the listener can immediately wait for the next one
            ThreadPool.QueueUserWorkItem((_) => HandleRequest(context));
        }
    }
    catch (HttpListenerException)
    {
        // Completely expected when _httpListener.Stop() is called on app close.
    }
    catch (Exception ex)
    {
        // If it fails to bind (usually needs Run As Administrator), actually show us the error!
        Dispatcher.Invoke(() => 
            MessageBox.Show($"Cue's ear is blocked!\n\nListener Error: {ex.Message}\n\nTry running the app as Administrator.", "Server Error"));
    }
    finally
    {
        if (_httpListener != null && _httpListener.IsListening)
        {
            _httpListener.Stop();
            _httpListener.Close();
        }
    }
}

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                if (context.Request.HttpMethod == "POST")
                {
                    string? jsonData = null;

                    using (StreamReader reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                    {
                        jsonData = reader.ReadToEnd();
                    }

                    // Validate JSON
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        try
                        {
                            JsonDocument.Parse(jsonData);
                            // Dispatch to UI thread
                            Dispatcher.Invoke(() => UpdateCueUI(jsonData));

                            // Send success response
                            context.Response.StatusCode = 200;
                        }
                        catch (JsonException)
                        {
                            context.Response.StatusCode = 400;
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    context.Response.StatusCode = 405; // Method Not Allowed
                }

                context.Response.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling request: {ex.Message}");
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch { }
            }
        }

        private void UpdateCueUI(string jsonData)
        {
            try
            {
                if (WebView.CoreWebView2 != null)
                {
                    // Escape the JSON string for JavaScript
                    string escapedJson = jsonData.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
                    string script = $"updateCue(\"{escapedJson}\");";
                    WebView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing script: {ex.Message}");
            }
        }
private string GetHtmlContent()
{
    return @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Cue</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            width: 100%;
            height: 100%;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: flex-start;
            padding: 40px 20px 20px 20px; /* Extra top padding for the hat */
            background: transparent;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            overflow: hidden;
        }

        .container {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 20px;
            width: 100%;
            height: 100%;
            max-width: 360px;
        }

      /* Face Puppet */
        .face {
            width: 200px;
            height: 200px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
            
            /* Add these two lines so it feels draggable */
            cursor: grab; 
        }
        
        /* Add this block right below .face */
        .face:active {
            cursor: grabbing;
        }

 /* The Plaid Scally Cap - Lightened for Dark Mode visibility */
        .scally-cap {
            position: absolute;
            top: -25px;
            width: 210px;
            height: 85px;
            /* Classic Grey Tweed Pattern */
            background: repeating-linear-gradient(
                45deg,
                #8b8e98,
                #8b8e98 15px,
                #6b6e78 15px,
                #6b6e78 30px
            );
            border-radius: 120px 120px 20px 20px;
            z-index: 10;
            /* Added a subtle light halo/outline so it pops on pitch-black backgrounds */
            box-shadow: inset 0 -10px 20px rgba(0,0,0,0.5), 0 0 10px rgba(255,255,255,0.15), 0 8px 15px rgba(0,0,0,0.4);
            border: 1px solid rgba(255, 255, 255, 0.1);
        }

        /* Cap Button */
        .scally-cap::before {
            content: '';
            position: absolute;
            top: 0;
            left: 50%;
            transform: translateX(-50%);
            width: 18px;
            height: 8px;
            background: #4a4d58;
            border-radius: 10px 10px 0 0;
        }

        /* Cap Brim */
        .scally-cap .brim {
            position: absolute;
            bottom: -8px;
            left: 50%;
            transform: translateX(-50%);
            width: 140px;
            height: 25px;
            background: #4a4d58;
            border-radius: 0 0 60px 60px;
            box-shadow: 0 6px 10px rgba(0,0,0,0.5);
            border-bottom: 2px solid #2a2d38;
            border-top: 1px solid rgba(255, 255, 255, 0.2);
        }

        .face.idle { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
        .face.thinking { background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); animation: pulse-thinking 1.5s ease-in-out infinite; }
        .face.speaking { background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%); animation: pulse-speaking 0.5s ease-in-out; }
        .face.alert { background: linear-gradient(135deg, #fa709a 0%, #fee140 100%); animation: pulse-alert 0.4s ease-in-out infinite; }

        @keyframes pulse-thinking { 0%, 100% { transform: scale(1); } 50% { transform: scale(1.05); } }
        @keyframes pulse-speaking { 0%, 100% { transform: scale(1); } 50% { transform: scale(1.08); } }
        @keyframes pulse-alert { 0%, 100% { transform: scale(1); box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1); } 50% { transform: scale(1.03); box-shadow: 0 4px 20px rgba(250, 112, 154, 0.4); } }

        /* Face Elements */
        .eyes {
            display: flex;
            gap: 30px;
            position: absolute;
            top: 70px;
            z-index: 5;
        }

        .eye {
            width: 24px;
            height: 24px;
            border-radius: 50%;
            background: white;
            position: relative;
            overflow: hidden;
            box-shadow: inset 0 2px 5px rgba(0,0,0,0.3);
        }

        .pupil {
            width: 12px;
            height: 12px;
            background: #111;
            border-radius: 50%;
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            transition: transform 0.1s ease-out; /* Smooth tracking */
        }

        .mouth {
            position: absolute;
            bottom: 45px;
            width: 60px;
            height: 30px;
            border: 3px solid white;
            border-top: none;
            border-radius: 0 0 60px 60px;
        }

        .mouth.speaking { border-width: 4px; animation: mouth-open 0.3s ease-in-out infinite; }

        @keyframes mouth-open { 0%, 100% { height: 30px; } 50% { height: 40px; } }

        /* Speech Bubble */
        .speech-bubble {
            background: white;
            border-radius: 15px;
            padding: 15px;
            font-size: 14px;
            text-align: center;
            color: #333;
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.15);
            position: relative;
            min-height: 40px;
            max-width: 100%;
            word-wrap: break-word;
            max-height: 120px;
            overflow-y: auto;
            opacity: 0;
            transition: opacity 0.3s ease, transform 0.3s ease;
            transform: translateY(-10px);
        }

        .speech-bubble.visible { opacity: 1; transform: translateY(0); }

        .speech-bubble::after {
            content: '';
            position: absolute;
            top: -10px;
            left: 50%;
            transform: translateX(-50%);
            border-left: 10px solid transparent;
            border-right: 10px solid transparent;
            border-bottom: 10px solid white;
        }

        /* Rich Content */
        .rich-content {
            background: rgba(255, 255, 255, 0.95);
            border-radius: 10px;
            padding: 15px;
            width: 100%;
            max-width: 100%;
            max-height: 150px;
            overflow-y: auto;
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
            opacity: 0;
            transition: opacity 0.3s ease, transform 0.3s ease;
            transform: translateY(-10px);
            display: none;
        }

        .rich-content.visible { opacity: 1; transform: translateY(0); display: block; }
        
        /* Scrollbars */
        .speech-bubble::-webkit-scrollbar, .rich-content::-webkit-scrollbar { width: 6px; }
        .speech-bubble::-webkit-scrollbar-track, .rich-content::-webkit-scrollbar-track { background: #f1f1f1; border-radius: 10px; }
        .speech-bubble::-webkit-scrollbar-thumb, .rich-content::-webkit-scrollbar-thumb { background: #667eea; border-radius: 10px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""face idle"">
            <div class=""scally-cap"">
                <div class=""brim""></div>
            </div>
            <div class=""eyes"">
                <div class=""eye""><div class=""pupil""></div></div>
                <div class=""eye""><div class=""pupil""></div></div>
            </div>
            <div class=""mouth""></div>
        </div>

        <div class=""speech-bubble""></div>
        <div class=""rich-content""></div>
    </div>

    <script>
        let currentEmotion = 'idle';

        function updateCue(jsonString) {
            try {
                const data = JSON.parse(jsonString);
                
                if (data.emotion_state) {
                    currentEmotion = data.emotion_state;
                    updateEmotionState(data.emotion_state);
                }
                
                if (data.dialogue !== undefined) updateDialogue(data.dialogue);
                if (data.html_payload !== undefined) updateRichContent(data.html_payload);
                
                // Reset eyes if we aren't idle
                if(currentEmotion !== 'idle') {
                    document.querySelectorAll('.pupil').forEach(p => {
                        p.style.transform = 'translate(-50%, -50%)';
                    });
                }
            } catch (error) {
                console.error('Error parsing JSON:', error);
            }
        }

        function updateEmotionState(emotionState) {
            const faceElement = document.querySelector('.face');
            const mouthElement = document.querySelector('.mouth');
            
            faceElement.classList.remove('idle', 'thinking', 'speaking', 'alert', 'speaking_happy');
            mouthElement.classList.remove('speaking');
            
            // Map speaking_happy to standard speaking animation
            const cssClass = emotionState === 'speaking_happy' ? 'speaking' : emotionState;
            faceElement.classList.add(cssClass);
            
            if (cssClass === 'speaking') {
                mouthElement.classList.add('speaking');
            }
        }

        function updateDialogue(dialogueText) {
            const speechBubble = document.querySelector('.speech-bubble');
            if (dialogueText && dialogueText.trim() !== '') {
                speechBubble.textContent = dialogueText;
                speechBubble.classList.add('visible');
            } else {
                speechBubble.classList.remove('visible');
                speechBubble.textContent = '';
            }
        }

        function updateRichContent(htmlPayload) {
            const richContent = document.querySelector('.rich-content');
            if (htmlPayload && htmlPayload.trim() !== '') {
                richContent.innerHTML = htmlPayload;
                richContent.classList.add('visible');
            } else {
                richContent.classList.remove('visible');
                richContent.innerHTML = '';
            }
        }

        // Mouse Tracking Logic
        document.addEventListener('mousemove', (e) => {
            if (currentEmotion !== 'idle') return; // Only track when idle
            
            const eyes = document.querySelectorAll('.eye');
            eyes.forEach(eye => {
                const rect = eye.getBoundingClientRect();
                const eyeCenterX = rect.left + rect.width / 2;
                const eyeCenterY = rect.top + rect.height / 2;

                const angle = Math.atan2(e.clientY - eyeCenterY, e.clientX - eyeCenterX);
                
                // Calculate distance and cap it so the pupil stays inside the eye
                const maxDistance = rect.width / 4; 
                const cursorDistance = Math.hypot(e.clientX - eyeCenterX, e.clientY - eyeCenterY) / 15;
                const distance = Math.min(maxDistance, cursorDistance);

                const x = Math.cos(angle) * distance;
                const y = Math.sin(angle) * distance;

                const pupil = eye.querySelector('.pupil');
                pupil.style.transform = `translate(calc(-50% + ${x}px), calc(-50% + ${y}px))`;
            });
        });

        // Initialize
        document.addEventListener('DOMContentLoaded', () => {
            const faceElement = document.querySelector('.face');
            faceElement.classList.add('idle');

            // ADD THIS BLOCK: Tell C# to drag the window when the face is clicked
            faceElement.addEventListener('mousedown', (e) => {
                // e.button === 0 ensures we only drag on Left Click
                if (e.button === 0 && window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage('drag_window');
                }
            });
        });
    </script>
</body>
</html>";
}

        protected override void OnClosed(EventArgs e)
        {
            // Stop the HTTP listener
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }

            if (_listenerThread != null && _listenerThread.IsAlive)
            {
                _listenerThread.Join(TimeSpan.FromSeconds(2));
            }

            _httpListener?.Close();

            base.OnClosed(e);
        }
    }
}
