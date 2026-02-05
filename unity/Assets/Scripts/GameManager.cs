using System;
using System.Globalization;
using OsuParser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager :MonoBehaviour
{
    public static GameManager Instance;

    [Header("Note Settings")] public NoteObject shortNotePrefab;
    public NoteObject longNotePrefab;
    public float offsetMs = 100f; // offset to sync with music
    public float noteStart = 9f;
    public float leadTimeMs = 2000f; // how early to spawn before it reaches button

    [Header("Score Settings")] public int currentScore;
    public int scorePerNote = 100;
    public int scorePerGoodNote = 125;
    public int scorePerPerfectNote = 150;

    [Header("Multiplier Settings")] public int currentMultiplier;
    public int multiplierTracker;
    public int[] multiplierThresholds;

    [Header("UI Elements")] public TextMeshProUGUI scoreTxt;
    public TextMeshProUGUI multiTxt;

    [Header("Results UI")] public GameObject menuScreen;
    public GameObject resultsScreen;
    public TextMeshProUGUI percentHitTxt;
    public TextMeshProUGUI normalHitTxt;
    public TextMeshProUGUI goodHitTxt;
    public TextMeshProUGUI perfectHitTxt;
    public TextMeshProUGUI missedHitTxt;
    public TextMeshProUGUI rankTxt;
    public TextMeshProUGUI finalScoreText;

    [Header("Next Level Button")] public Button nextLevelButton;
    public GameObject inputButton;
    public LevelManager levelManager;
    public AudioSource music;

    // [Header("Stats Tracking")]
    float _totalNotes;
    float _normalHits;
    float _goodHits;
    float _perfectHits;
    float _missedHits;

    bool _levelLoaded;

    bool _isHoldingNote;

    NoteSpawner _noteSpawner;
    LevelLoader _levelLoader;

    // Bluetooth stuff
    readonly LightstickInput _lightstickInput = new();

    void Start()
    {
        // Debug.Log(
        // BleConnection.Instance.controllerConnected ? "Controller connected!" : "Controller not connected.");
        // 240FPS for notes spawning
        Time.fixedDeltaTime = 1f / 240f;

        Instance = this;
        _levelLoader = gameObject.AddComponent<LevelLoader>();
        _noteSpawner = gameObject.AddComponent<NoteSpawner>();
        _lightstickInput.button = inputButton.GetComponent<ButtonController>();

        scoreTxt.text = "Score: 0";
        currentMultiplier = 1;

        _isHoldingNote = false;
        resultsScreen.SetActive(false);
        nextLevelButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if(!_levelLoaded) return;

        if(_isHoldingNote)
        {
            HoldStart();
        }
        else
        {
            HoldEnd();
        }

        if(resultsScreen.activeSelf || music.isPlaying) return;
        Debug.Log("Level ended, showing results...");
        ShowResults();
    }

    void PollController()
    {
        while (BleApi.PollData(out var res, false))
        {
            // Debug.Log("Polling controller...");
            LightStickPacket packet;
            packet.delay = BitConverter.ToInt32(res.buf, 0);
            packet.data = res.buf[4];
            // Debug.Log("Delay: " + packet.delay + "us");

            _lightstickInput.UpdateFromPacket(packet);
        }
    }

    void OnLevelReady(OsuBeatmap osuBeatmap)
    {
        _levelLoaded = true;
        music.clip = osuBeatmap.audioClip;
        music.Play();
        music.loop = false;
        menuScreen.SetActive(false);
        _noteSpawner.transform.position = Vector3.up * noteStart;
        _noteSpawner.longNotePrefab = longNotePrefab;
        _noteSpawner.shortNotePrefab = shortNotePrefab;
        _noteSpawner.offsetMs = offsetMs;
        _noteSpawner.noteStart = noteStart;
        _noteSpawner.leadTimeMs = leadTimeMs;
        _noteSpawner.Initialize(osuBeatmap, music);
        _totalNotes = osuBeatmap.notesCount;
    }

    void ShowResults()
    {
        _isHoldingNote = false;

        resultsScreen.SetActive(true);

        normalHitTxt.text = _normalHits.ToString(CultureInfo.InvariantCulture);
        goodHitTxt.text = _goodHits.ToString(CultureInfo.InvariantCulture);
        perfectHitTxt.text = _perfectHits.ToString(CultureInfo.InvariantCulture);
        missedHitTxt.text = _missedHits.ToString(CultureInfo.InvariantCulture);

        var totalHit = _normalHits + _goodHits + _perfectHits;
        var percentHit = (_totalNotes > 0) ? (totalHit / (_totalNotes + _missedHits)) * 100f : 0f;
        percentHitTxt.text = percentHit.ToString("F1") + "%";

        var rankVal = percentHit switch
        {
            > 95 => "S",
            > 85 => "A",
            > 70 => "B",
            > 55 => "C",
            > 40 => "D",
            _ => "F"
        };
        rankTxt.text = rankVal;

        finalScoreText.text = currentScore.ToString();
        nextLevelButton.gameObject.SetActive(true);
    }

    // --- Hit & Hold Notes Programmatically ---
    public static void HitNote()
    {
        // Only hit latest note that can be pressed
        NoteObject latest = null;

        foreach (var n in FindObjectsByType<NoteObject>(FindObjectsSortMode.None))
        {
            if(!n.CanBePressed() || n.noteType != NoteType.Short) continue;

            if(!latest || n._lifetimeMs < latest._lifetimeMs)
            {
                latest = n;
            }
        }

        latest?.Pressed();
    }

    public void HoldStart()
    {
        _isHoldingNote = true;

        foreach (var n in FindObjectsByType<NoteObject>(FindObjectsSortMode.None))
        {
            // Start all holds that can be started
            if(n._lifetimeMs > 0
               || n.noteType != NoteType.Long
               || n.isBeingHeld) continue;
            n.HoldStart();
        }
    }

    public void HoldEnd()
    {
        _isHoldingNote = false;

        foreach (var noteObject in FindObjectsByType<NoteObject>(FindObjectsSortMode.None))
        {
            // End all held notes
            if(noteObject._lifetimeMs > 0
               || !noteObject.isBeingHeld
               || noteObject.noteType != NoteType.Long) continue;
            noteObject.HoldEnd();
        }
    }

    // --- Scoring System ---
    void NoteHit()
    {
        if(currentMultiplier - 1 < multiplierThresholds.Length)
        {
            multiplierTracker++;

            if(multiplierThresholds[currentMultiplier - 1] <= multiplierTracker)
            {
                multiplierTracker = 0;
                currentMultiplier++;
            }
        }

        multiTxt.text = "Multiplier: x" + currentMultiplier;
        scoreTxt.text = "Score: " + currentScore;
    }

    public void NormalHit()
    {
        currentScore += scorePerNote * currentMultiplier;
        NoteHit();
        _normalHits++;
    }

    public void GoodHit()
    {
        currentScore += scorePerGoodNote * currentMultiplier;
        NoteHit();
        _goodHits++;
    }

    public void PerfectHit()
    {
        currentScore += scorePerPerfectNote * currentMultiplier;
        NoteHit();
        _perfectHits++;
    }

    public void NoteMissed()
    {
        currentMultiplier = 1;
        multiplierTracker = 0;
        multiTxt.text = "Multiplier: x" + currentMultiplier;
        _missedHits++;
    }

    public void LoadLevel()
    {
        if (!BleConnection.Instance.controllerConnected || _levelLoaded) return;
        music.Stop();
        _levelLoader.Load(levelManager.level, levelManager.difficulty, OnLevelReady);
    }

    public void ExitToStart()
    {
        music.Stop();
        _levelLoaded = false;
        resultsScreen.SetActive(false);
        menuScreen.SetActive(true);
    }
}
