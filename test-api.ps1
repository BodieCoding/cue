#!/usr/bin/env pwsh
# Test script for Cue HTTP API

param(
    [Parameter(Mandatory=$false)]
    [string]$EmotionState = "idle",
    
    [Parameter(Mandatory=$false)]
    [string]$Dialogue = "Hello from test script!",
    
    [Parameter(Mandatory=$false)]
    $HtmlPayload = $null
)

$url = "http://127.0.0.1:8081/update"

$body = @{
    emotion_state = $EmotionState
    dialogue = $Dialogue
    html_payload = $HtmlPayload
} | ConvertTo-Json -Compress 

Write-Host "Sending request to $url"
Write-Host "Payload: $body"
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $url `
        -Method POST `
        -ContentType "application/json; charset=utf-8" `
        -Body $body `
        -ErrorAction Stop
    
    Write-Host "[SUCCESS] Cue received the command!" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}