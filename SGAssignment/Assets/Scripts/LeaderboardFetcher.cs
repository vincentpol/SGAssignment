using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class LeaderboardFetcher : MonoBehaviour
{
    // Classes to fetch data
    [Serializable]
    private record Metadata
    {
        public int page = 0;
        public int page_count = 0;
    }

    [Serializable]
    private record Record
    {
        public int rank = 0;
        public int score = 0;
        public string player_name = string.Empty;
        public string avatar_url = string.Empty;

        public bool IsValid()
        {
            return rank > 0
                && score > 0
                && !string.IsNullOrEmpty(player_name)
                && !string.IsNullOrEmpty(avatar_url);
        }
    }

    [Serializable]
    private record Page
    {
        public Metadata _metadata = null;
        public List<Record> records = null;
    }

    private const string UrlFormat = "https://private-24ba57-softgamesleaderboard.apiary-mock.com/leaderboard?page={0}";

    public int MaxKnownRank { get; private set; }
    public Action OnMaxKnownRankChanged;

    private int _knownPages = 0;
    private Dictionary<int, LeaderboardEntry> _cachedData = new();

    private void Awake()
    {
        // Start loading pages
        _ = LoadPages();
    }

    private async Task LoadPages()
    {
        // Gather first page to get meta data
        await FetchPageDataAsync(1);

        // Load additional pages now knowing how many pages there are
        for (var i = 2; i <= _knownPages; i++)
        {
            await FetchPageDataAsync(i);
        }

        Debug.Log("Finished downloading all pages");
    }

    public LeaderboardEntry GetEntry(int rankId)
    {
        return _cachedData.TryGetValue(rankId, out var entry)
            ? entry
            : null;
    }

    private async Task FetchPageDataAsync(int pageId = 1)
    {
        int currentFailedAttempts = 0;
        float currentDelayTimeInSeconds = 1f;

        while (true)
        {
            try
            {
                // Get url based on page id
                var finalUrl = string.Format(UrlFormat, pageId);

                // Start web request
                using (var request = UnityWebRequest.Get(finalUrl))
                {
                    await request.SendWebRequest();

                    // Error handling
                    if (request.result == UnityWebRequest.Result.ConnectionError ||
                        request.result == UnityWebRequest.Result.DataProcessingError ||
                        request.result == UnityWebRequest.Result.ProtocolError)
                    {
                        throw new Exception($"Error fetching data: {request.error}");
                    }

                    var json = request.downloadHandler.text;

                    // If our json is invalid, throw an exception
                    if (string.IsNullOrEmpty(json))
                        throw new Exception("Returned JSON is empty");

                    // Convert to page
                    var page = ProcessJson(json);

                    // If it's invalid, throw an exception
                    if (page == null)
                        throw new Exception("Page object is null");

                    int maxRank = int.MinValue;
                    foreach (var record in page.records)
                    {
                        // Ignore any invalid record
                        if (!record.IsValid())
                            continue;

                        if (maxRank < record.rank)
                            maxRank = record.rank;

                        // Save it
                        LeaderboardEntry entry;
                        if (!_cachedData.ContainsKey(record.rank))
                        {
                            entry = new();
                            _cachedData.Add(record.rank, entry);
                        }
                        else
                        {
                            entry = _cachedData[record.rank];
                        }

                        // Update data
                        entry.UpdateData(record.rank, record.score, record.player_name, record.avatar_url);
                    }

                    // Save known amount of pages
                    var knownPages = page._metadata?.page_count ?? 1;

                    if (_knownPages != knownPages)
                        _knownPages = knownPages;

                    // Update max known rank
                    if (maxRank > MaxKnownRank)
                    {
                        MaxKnownRank = maxRank;
                        OnMaxKnownRankChanged?.Invoke();
                    }

                    return;
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
                    return;
                }

                // Wait before retrying
                await Task.Delay(TimeSpan.FromSeconds(currentDelayTimeInSeconds));

                // Double retry time for next attempt
                currentDelayTimeInSeconds *= 2f;
            }
        }
    }

    private static Page ProcessJson(string json)
    {
        try
        {
            // Get the json before we fix it
            var preFixJson = json;

            // Fix any trainling commas
            json = FixTrailingCommas(json);

            // If the outcome doesn't match, throw a warning
            if (preFixJson.Length != json.Length)
                Debug.LogWarning("Json string had one or multiple trailing commas. An attempt to correct it has been made, but this should be addressed!");

            return JsonUtility.FromJson<Page>(json);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Error processing json: {exception}");
            return null;
        }
    }

    private static string FixTrailingCommas(string json)
    {
        return Regex.Replace(json, @"(,)(\s*[}\]])", "$2");
    }
}