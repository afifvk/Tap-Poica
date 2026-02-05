using System.Collections.Generic;
using System.Linq;
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

public class NoteSpawner : MonoBehaviour
{
    public NoteObject shortNotePrefab;
    public NoteObject longNotePrefab;

    public float offsetMs; // offset to sync with music
    public float noteStart;
    public float leadTimeMs; // how early to spawn before it reaches button
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
        if (!osuBeatmap.audioClip) return;
        _audioSource.clip = osuBeatmap.audioClip;

        foreach (var noteData in _notes.Where(noteData => leadTimeMs > noteData.timeMs + offsetMs))
        {
            SpawnNote(noteData);
            _nextIndex++;
        }
    }

    void FixedUpdate()
    {
        if (_notes == null || _bpm == 0) return;
        if (_nextIndex >= _notes.Count) return;

        var musicTimeMs = _audioSource.time * 1000;

        while (_nextIndex < _notes.Count && leadTimeMs >= _notes[_nextIndex].timeMs - musicTimeMs + offsetMs)
        {
            SpawnNote(_notes[_nextIndex]);
        }
    }

    void SpawnNote(NoteData data)
    {
        var prefabToSpawn = data.type == NoteType.Long ? longNotePrefab : shortNotePrefab;

        if (!prefabToSpawn)
        {
            // Debug.LogError("Prefab to spawn is not assigned!");
            return;
        }

        var noteObject = Instantiate(prefabToSpawn, transform.position, Quaternion.identity, transform);

        if (!noteObject)
        {
            // Debug.LogError("Spawned object does not have a NoteObject component!");
            return;
        }

        // var musicTimeMs = _audioSource.time * 1000;
        noteObject.Initialize(data, noteStart / leadTimeMs * 1000f, leadTimeMs);

        _nextIndex++;
    }
}
