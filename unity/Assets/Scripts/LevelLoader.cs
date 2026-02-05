using System;
using System.Collections;
using System.IO;
using OsuParser;
using UnityEngine;
using UnityEngine.Networking;

public class LevelLoader :MonoBehaviour
{
    public void Load(Level level, LevelDifficulty difficultyIndex, Action<OsuBeatmap> onLoaded)
    {
        // Debug.Log($"LevelLoader: Requested load for {level} at difficulty {difficultyIndex}");
        var levelMetadata = LevelData.LevelRegistry[(int)level];

        if(levelMetadata == null)
        {
            // Debug.LogError($"LevelLoader: Could not find level named '{level}'");
            return;
        }

        if(difficultyIndex < 0 || difficultyIndex >= (LevelDifficulty)levelMetadata.difficulties)
        {
            // Debug.LogError($"LevelLoader: Difficulty index {difficultyIndex} is out of range for {level}");
            return;
        }

        StartCoroutine(LoadRoutine(level, difficultyIndex, onLoaded));
    }

    // --- 3. The Internal Logic ---

    IEnumerator LoadRoutine(Level level, LevelDifficulty difficulty, Action<OsuBeatmap> callback)
    {
        var metadata = LevelData.LevelRegistry[(int)level];
        // Construct path: StreamingAssets/irisout/1.osu
        var mapPath = Path.Combine(Application.streamingAssetsPath, metadata.folderName, (int)difficulty + ".osu");

        OsuBeatmap loadedOsuBeatmap;

        if(!mapPath.Contains("://"))
            mapPath = "file://" + mapPath;

        using (var www = UnityWebRequest.Get(mapPath))
        {
            yield return www.SendWebRequest();

            // if(www.result != UnityWebRequest.Result.Success)
                // Debug.LogError($"LevelLoader Error: {www.error} at path {mapPath}");

            // Debug.Log("LevelLoader: File read. Parsing...");

            // Parse it using the OsuParser we made earlier
            loadedOsuBeatmap = FileParser.Parse(www.downloadHandler.text);
        }

        StartCoroutine(LoadAudioClip(level, audioClip =>
        {
            loadedOsuBeatmap.audioClip = audioClip;
            callback?.Invoke(loadedOsuBeatmap);
        }));
    }

    public static IEnumerator LoadAudioClip(Level level, Action<AudioClip> callback)
    {
        var metadata = LevelData.LevelRegistry[(int)level];

        var audioPath = Path.Combine(Application.streamingAssetsPath, metadata.folderName,
            $"audio.{metadata.fileExtension}");

        using var wwwAudio = UnityWebRequestMultimedia.GetAudioClip(
            "file://" + audioPath, metadata.audioType);

        yield return wwwAudio.SendWebRequest();

        // if(wwwAudio.result != UnityWebRequest.Result.Success)
            // Debug.LogError($"LevelLoader Audio Error: {wwwAudio.error} at path {audioPath}");

        callback?.Invoke(wwwAudio.result == UnityWebRequest.Result.Success
            ? DownloadHandlerAudioClip.GetContent(wwwAudio)
            : null);
    }
}
