#!/usr/bin/env pwsh
# Complete Demo script for Cue HTTP API

# 1. Define the function that actually sends the data to Cue
function Send-CueUpdate {
    param(
        [string]$EmotionState,
        [string]$Dialogue,
        $HtmlPayload
    )

    $url = "http://127.0.0.1:8081/update"

    # Create the JSON payload, compressing it to avoid multi-line parsing errors
    $body = @{
        emotion_state = $EmotionState
        dialogue = $Dialogue
        html_payload = $HtmlPayload
    } | ConvertTo-Json -Compress 

    Write-Host "Triggering Cue: [$EmotionState]" -ForegroundColor Cyan

    try {
        $response = Invoke-RestMethod -Uri $url `
            -Method POST `
            -ContentType "application/json; charset=utf-8" `
            -Body $body `
            -ErrorAction Stop
    }
    catch {
        Write-Host "✗ Error connecting to Cue: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Make sure your C# app is running!" -ForegroundColor Yellow
    }
}

Write-Host "Starting Cue Demo..." -ForegroundColor Green
Write-Host "--------------------"

# 2. Define our safe HTML Here-String
$htmlExample = @"
<div style="font-family: sans-serif; padding: 10px; border: 1px solid #ccc; border-radius: 5px; background: rgba(255,255,255,0.8);">
    <h3 style="margin-top: 0; color: #333;">Pipeline Status</h3>
    <p>This is <b>HTML content</b> safely injected into the display.</p>
    <ul style="margin-bottom: 0;">
        <li>DB Connection: <span style="color: green;">OK</span></li>
        <li>Memory: 42%</li>
    </ul>
</div>
"@

# 3. Run the sequence (with pauses so you can see the animations)
Send-CueUpdate "speaking_happy" "Hi! I am Cue, your desktop assistant. Here is a look at that rich content panel!" $htmlExample
Start-Sleep -Seconds 4

Send-CueUpdate "thinking" "Hmm... let me check those logs for you..." $null
Start-Sleep -Seconds 3

Send-CueUpdate "alert" "Whoa! Something just failed in the pipeline!" '<div style="color: red; font-weight: bold; background: white; padding: 5px;">ERROR 500: Database Timeout</div>'
Start-Sleep -Seconds 3

Send-CueUpdate "celebrating" "Wait, nevermind! The auto-recovery worked. We are back online!" $null
Start-Sleep -Seconds 3

Send-CueUpdate "idle" "Standing by for your next command." $null

Write-Host "--------------------"
Write-Host "Demo complete!" -ForegroundColor Green