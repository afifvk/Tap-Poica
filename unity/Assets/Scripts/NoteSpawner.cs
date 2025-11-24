using UnityEngine;

[System.Serializable]
public class NoteData
{
    public float time;      // when the note should be hit (seconds)
    public bool isLongNote;
}

public class NoteSpawner : MonoBehaviour
{
    public GameObject shortNotePrefab;
    public GameObject longNotePrefab;
    public NoteData[] notes;
    public float spawnLeadTime = 2f;   // how early to spawn before it reaches button
    public AudioSource music;

    private int nextIndex = 0;

    void Update()
    {
        if (nextIndex >= notes.Length) return;

        float songTime = music.time;

        if (songTime + spawnLeadTime >= notes[nextIndex].time)
        {
            var data = notes[nextIndex];

            GameObject prefabToSpawn = data.isLongNote ? longNotePrefab : shortNotePrefab;
            Instantiate(prefabToSpawn, transform.position, Quaternion.identity);

            nextIndex++;
        }
    }
}