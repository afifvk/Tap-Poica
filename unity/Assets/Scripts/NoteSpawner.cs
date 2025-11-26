using System;
using System.Collections.Generic;
using OsuParser;
using UnityEngine;

public enum NoteType
{
    Short,
    Long
}

// this is the clean data class the parser/converter produces
public class NoteData
{
    public double timeMs; // when it hits the line (ms)
    public NoteType type; // the enum
    public double durationMs; // in ms (0 for short notes)
}

public class NoteSpawner :MonoBehaviour
{
    public GameObject shortNotePrefab;
    public GameObject longNotePrefab;

    public float noteSpeed = 1f;
    public float leadTimeMs = 2000f; // how early to spawn before it reaches button
    List<NoteData> _notes;
    AudioSource _audioSource;

    int _nextIndex;
    double _bpm;
    double _beatLength;

    public void Initialize(OsuBeatmap osuBeatmap, AudioSource source)
    {
        Debug.Log("Initializing NoteSpawner with beatmap data.");
        _bpm = osuBeatmap.globalBpm;
        _audioSource = source;
        _notes = NoteConverter.Convert(osuBeatmap);
        if(!osuBeatmap.audioClip) return;
        _audioSource.clip = osuBeatmap.audioClip;
    }

    void Update()
    {
        // Debug.Log($"{_notes} notes. BPM: {_bpm}");
        if(_notes == null || _bpm == 0) return;
        if(_nextIndex >= _notes.Count) return;

        var musicTimeMs = _audioSource.time * 1000;

        // Debug.Log($"Music Time: {musicTimeMs} ms, Lead Time: {leadTimeMs}, Next Note Time: {_notes[_nextIndex].timeMs} ms");
        var data = _notes[_nextIndex];
        if(musicTimeMs + leadTimeMs <= data.timeMs) return;

        var prefabToSpawn = data.type == NoteType.Long ? longNotePrefab : shortNotePrefab;

        if(!prefabToSpawn)
        {
            Debug.LogError("Prefab to spawn is not assigned!");
            return;
        }
        var gameObj = Instantiate(prefabToSpawn, transform.position, Quaternion.identity, transform);
        var noteObject = gameObj.GetComponent<NoteObject>();
        if (!noteObject)
        {
            Debug.LogError("Spawned object does not have a NoteObject component!");
            return;
        }
        noteObject.Initialize(data, noteSpeed * (float)_bpm / 60f);

        _nextIndex++;
    }
}
