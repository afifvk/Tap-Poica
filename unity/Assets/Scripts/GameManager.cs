using System.Globalization;
using OsuParser;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager :MonoBehaviour
{
    public static GameManager Instance;

    [Header("Level Settings")]
    public LevelData levelData;

    [Header("Note Settings")] public GameObject shortNotePrefab;
    public GameObject longNotePrefab;
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

    [Header("Results UI")] public GameObject resultsScreen;
    public TextMeshProUGUI percentHitTxt;
    public TextMeshProUGUI normalHitTxt;
    public TextMeshProUGUI goodHitTxt;
    public TextMeshProUGUI perfectHitTxt;
    public TextMeshProUGUI missedHitTxt;
    public TextMeshProUGUI rankTxt;
    public TextMeshProUGUI finalScoreText;

    [Header("Next Level Button")] public Button nextLevelButton;

    // [Header("Stats Tracking")]
    float _totalNotes;
    float _normalHits;
    float _goodHits;
    float _perfectHits;
    float _missedHits;
    bool _levelLoaded;
    bool _startingPoint;
    bool _resultsShown;

    NoteSpawner _noteSpawner;
    LevelLoader _levelLoader;
    AudioSource _music;

    void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 240FPS for notes spawning
        Time.fixedDeltaTime = 1f / 240f;

        Instance = this;
        _levelLoader = gameObject.AddComponent<LevelLoader>();
        _levelLoader.Load(levelData.level, levelData.difficulty, OnLevelReady);
        _noteSpawner = gameObject.AddComponent<NoteSpawner>();
        _music = gameObject.AddComponent<AudioSource>();

        scoreTxt.text = "Score: 0";
        currentMultiplier = 1;

        _totalNotes = FindObjectsByType<NoteObject>(FindObjectsSortMode.None).Length;

        resultsScreen.SetActive(false);
        // nextLevelButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if(!_levelLoaded) return;

        if(_startingPoint || Input.anyKeyDown) return;
        _startingPoint = true;
        // beatScroller.hasStarted = true;

        if(_resultsShown || _music.isPlaying) return;
        ShowResults();
        _resultsShown = true;
    }

    void OnLevelReady(OsuBeatmap osuBeatmap)
    {
        _levelLoaded = true;
        _music.clip = osuBeatmap.audioClip;
        _music.Play();
        _music.loop = false;
        _noteSpawner.transform.position = Vector3.up * noteStart;
        _noteSpawner.longNotePrefab = longNotePrefab;
        _noteSpawner.shortNotePrefab = shortNotePrefab;
        _noteSpawner.offsetMs = offsetMs;
        _noteSpawner.noteStart = noteStart;
        _noteSpawner.leadTimeMs = leadTimeMs;
        _noteSpawner.Initialize(osuBeatmap, _music);
    }

    void ShowResults()
    {
        resultsScreen.SetActive(true);
        _resultsShown = true;

        normalHitTxt.text = _normalHits.ToString(CultureInfo.InvariantCulture);
        goodHitTxt.text = _goodHits.ToString(CultureInfo.InvariantCulture);
        perfectHitTxt.text = _perfectHits.ToString(CultureInfo.InvariantCulture);
        missedHitTxt.text = _missedHits.ToString(CultureInfo.InvariantCulture);

        var totalHit = _normalHits + _goodHits + _perfectHits;
        var percentHit = (_totalNotes > 0) ? (totalHit / _totalNotes) * 100f : 0f;
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
        // nextLevelButton.gameObject.SetActive(true);
    }

    // --- Hit & Hold Notes Programmatically ---
    public void HitNote()
    {
        var notes = FindObjectsByType<NoteObject>(FindObjectsSortMode.None);
        NoteObject closest = null;
        var bestDist = Mathf.Infinity;

        foreach (var n in notes)
        {
            var d = Mathf.Abs(n.transform.position.y);
            if(!n.canBePressed && d >= bestDist) continue;
            bestDist = d;
            closest = n;
        }

        if(!closest) return;
        closest.Pressed();
    }

    public void HoldStart()
    {
        foreach (var n in FindObjectsByType<NoteObject>(FindObjectsSortMode.None))
        {
            if(!n.canBePressed || n.noteType != NoteType.Long) continue;
            n.HoldStart();
            return;
        }
    }

    public void HoldEnd()
    {
        foreach (var n in FindObjectsByType<NoteObject>(FindObjectsSortMode.None))
        {
            if(!n.isBeingHeld) continue;
            n.HoldEnd();
            return;
        }
    }

    // --- Scoring System ---
    public void NoteHit()
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

    // // ----------------------------------------------------------
    // // NEXT LEVEL BUTTON → Load next scene
    // // ----------------------------------------------------------
    //     public void LoadNextLevel()
    //     {
    //         Time.timeScale = 1f;
    //
    //         var currentIndex = SceneManager.GetActiveScene().buildIndex;
    //         var nextIndex = currentIndex + 1;
    //
    //         if(nextIndex < SceneManager.sceneCountInBuildSettings)
    //         {
    //             SceneManager.LoadScene(nextIndex);
    //         }
    //         else
    //         {
    //             Debug.Log("Reached the last level!");
    //         }
    //     }
    //
    // // ----------------------------------------------------------
    // // EXIT LAST LEVEL → Load specific scene (e.g. Main Menu)
    // // ----------------------------------------------------------
    //     public void ExitToScene(string sceneName)
    //     {
    //         Time.timeScale = 1f;
    //         SceneManager.LoadScene(sceneName);
    //     }
}
