using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.Networking;

public class LeaderboardEntry
{
    public Action OnSpriteLoaded;

    public int Rank { get; private set; }
    public int Score { get; private set; }
    public string PlayerName { get; private set; }
    public string AvatarUrl { get; private set; }
    public Sprite Sprite { get; private set; }

    private bool _isInWaitingQueue;

    private static List<LeaderboardEntry> WaitingQueue = new();

    public void ChangeRank(int rank)
    {
        Rank = rank;
    }

    public void UpdateData(int score, string playerName, string avatarUrl)
    {
        Score = score;
        PlayerName = playerName;
        AvatarUrl = avatarUrl;
    }

    public void LoadImage()
    {
        // If we already have a sprite or are already in the queue, early out
        if (string.IsNullOrEmpty(AvatarUrl)
            || Sprite != null
            || _isInWaitingQueue)
            return;

        // Flag that we're in the queue
        _isInWaitingQueue = true;

        // Add this entry to the queue
        WaitingQueue.Add(this);

        // If we are not the current first in the queue, early out.
        if (WaitingQueue[0] != this)
            return;

        // Start loading image
        _ = DownloadAndRenderSVGAsync();
    }

    private async Task DownloadAndRenderSVGAsync()
    {
        Debug.Log($"Starting download {AvatarUrl}");

        int currentFailedAttempts = 0;
        float delayTimeInSeconds = 1f;

        while (true)
        {
            try
            {
                using (UnityWebRequest request = UnityWebRequest.Get(AvatarUrl))
                {
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.ConnectionError
                        || request.result == UnityWebRequest.Result.ProtocolError
                        || request.result == UnityWebRequest.Result.DataProcessingError)
                    {
                        throw new Exception($"Error downloading SVG: {request.error}");
                    }

                    var svgText = request.downloadHandler.text;

                    try
                    {
                        BuildSprite(svgText);
                    }
                    catch
                    {
                        throw new Exception($"Error Building sprite!");
                    }
                    break;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message);

                // Increment fail attempts and check if we've reached the limit
                currentFailedAttempts++;
                if (currentFailedAttempts >= Settings.MaxFailedConnectionAttempts)
                {
                    Debug.LogError("Max failed attempts reached. Aborting.");
                    break;
                }

                // Wait before retrying
                await Task.Delay(TimeSpan.FromSeconds(delayTimeInSeconds));

                // Double retry time for next attempt
                delayTimeInSeconds *= 2f;
            }
        }

        Debug.Log($"Finished downloading {AvatarUrl}");

        // Remove from queue
        WaitingQueue.RemoveAt(0);
        _isInWaitingQueue = false;

        // If another one is waiting
        if (WaitingQueue.Count > 0)
        {
            // Get it and run it
            var nextEntry = WaitingQueue[0];
            await nextEntry.DownloadAndRenderSVGAsync();
        }
    }

    private void BuildSprite(string svgText)
    {
        // Replace with custom image that will work
        svgText = @"<svg width=""283.9"" height=""283.9"" xmlns=""http://www.w3.org/2000/svg"">
            <line x1=""170.3"" y1=""226.99"" x2=""177.38"" y2=""198.64"" fill=""none"" stroke=""#888"" stroke-width=""1""/>
            <line x1=""205.73"" y1=""198.64"" x2=""212.81"" y2=""226.99"" fill=""none"" stroke=""#888"" stroke-width=""1""/>
            <line x1=""212.81"" y1=""226.99"" x2=""219.9"" y2=""255.33"" fill=""none"" stroke=""#888"" stroke-width=""1""/>
            <line x1=""248.25"" y1=""255.33"" x2=""255.33"" y2=""226.99"" fill=""none"" stroke=""#888"" stroke-width=""1""/>
            <path d=""M170.08,226.77c7.09-28.34,35.43-28.34,42.52,0s35.43,28.35,42.52,0"" transform=""translate(0.22 0.22)"" fill=""none"" stroke=""red"" stroke-width=""1.2""/>
            <circle cx=""170.3"" cy=""226.99"" r=""1.2"" fill=""blue"" stroke-width=""0.6""/>
            <circle cx=""212.81"" cy=""226.99"" r=""1.2"" fill=""blue"" stroke-width=""0.6""/>
            <circle cx=""255.33"" cy=""226.99"" r=""1.2"" fill=""blue"" stroke-width=""0.6""/>
            <circle cx=""177.38"" cy=""198.64"" r=""1"" fill=""black"" />
            <circle cx=""205.73"" cy=""198.64"" r=""1"" fill=""black"" />
            <circle cx=""248.25"" cy=""255.33"" r=""1"" fill=""black"" />
            <circle cx=""219.9"" cy=""255.33"" r=""1"" fill=""black"" />
        </svg>";

        // Get scene info
        var sceneInfo = SVGParser.ImportSVG(new StringReader(svgText), ViewportOptions.PreserveViewport);

        // Setup tesselation options
        var tessellationOptions = new VectorUtils.TessellationOptions
        {
            StepDistance = 100f,
            MaxCordDeviation = 0.5f,
            MaxTanAngleDeviation = 0.1f,
            SamplingStepSize = 0.01f,
        };

        // Generate geometry
        var geometry = VectorUtils.TessellateScene(sceneInfo.Scene, tessellationOptions);

        // Convert to sprite
        Sprite = VectorUtils.BuildSprite(geometry, 10f, VectorUtils.Alignment.Center, Vector2.zero, 128, true);

        // Inform that sprite is loaded
        OnSpriteLoaded?.Invoke();
    }
}
