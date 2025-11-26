using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using OsuParser;
using UnityEngine;
using UnityEngine.Networking;

public class LevelLoader :MonoBehaviour
{
    public void Load(Level level, LevelDifficulty difficultyIndex, Action<OsuBeatmap> onLoaded)
    {
        Debug.Log($"LevelLoader: Requested load for {level} at difficulty {difficultyIndex}");
        var levelMetadata = LevelData.LevelRegistry[(int)level];

        if(levelMetadata == null)
        {
            Debug.LogError($"LevelLoader: Could not find level named '{level}'");
            return;
        }

        if(difficultyIndex < 0 || difficultyIndex >= (LevelDifficulty)levelMetadata.difficulties)
        {
            Debug.LogError($"LevelLoader: Difficulty index {difficultyIndex} is out of range for {level}");
            return;
        }

        StartCoroutine(LoadRoutine(levelMetadata.folderName, difficultyIndex, onLoaded));
    }

// --- 3. The Internal Logic ---

    IEnumerator LoadRoutine(string folderName, LevelDifficulty difficultyIndex, Action<OsuBeatmap> callback)
    {
        // Construct path: StreamingAssets/irisout/1.osu
        var mapPath = Path.Combine(Application.streamingAssetsPath, folderName, difficultyIndex + ".osu");

        OsuBeatmap loadedOsuBeatmap;

        if(!mapPath.Contains("://"))
            mapPath = "file://" + mapPath;

        using (var www = UnityWebRequest.Get(mapPath))
        {
            yield return www.SendWebRequest();

            if(www.result != UnityWebRequest.Result.Success)
                Debug.LogError($"LevelLoader Error: {www.error}");

            Debug.Log("LevelLoader: File read. Parsing...");

            // Parse it using the OsuParser we made earlier
            loadedOsuBeatmap = FileParser.Parse(www.downloadHandler.text);
        }

        // Construct path: StreamingAssets/irisout/audio.mp3
        var audioPath = Path.Combine(Application.streamingAssetsPath, folderName, "audio.mp3");

        using (var wwwAudio = UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.MPEG))
        {
            yield return wwwAudio.SendWebRequest();

            loadedOsuBeatmap.audioClip = wwwAudio.result == UnityWebRequest.Result.Success
                ? DownloadHandlerAudioClip.GetContent(wwwAudio)
                : null;
            // Send the data back to whoever asked for it
        }

        callback?.Invoke(loadedOsuBeatmap);
    }

// Simple data class for your registry
}
