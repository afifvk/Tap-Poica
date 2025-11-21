using UnityEngine;
using UnityEngine.UI;

public class GameManager :MonoBehaviour
{
    public static GameManager Instance;

    [Header("Audio & Gameplay")] public AudioSource audioSource;
    public BeatScroller theBs;
    private bool _startingPoint;
    private bool _resultsShown = false;

    [Header("Score Settings")] public int currentScore;
    public int scorePerNote = 100;
    public int scorePerGoodNote = 125;
    public int scorePerPerfectNote = 150;

    [Header("Multiplier Settings")] public int currentMultiplier;
    public int multiplierTracker;
    public int[] multiplierThresholds;

    [Header("UI Elements")] public Text scoreTxt;
    public Text multiTxt;

    [Header("Results UI")] public GameObject resultsScreen;
    public Text percentHitTxt;
    public Text normalHitTxt;
    public Text goodHitTxt;
    public Text perfectHitTxt;
    public Text missedHitTxt;
    public Text rankTxt;
    public Text finalScoreText;

    [Header("Stats Tracking")] public float totalNotes;
    public float normalHits;
    public float goodHits;
    public float perfectHits;
    public float missedHits;

    void Start()
    {
        Instance = this;

        scoreTxt.text = "Score: 0";
        currentMultiplier = 1;

        totalNotes = FindObjectsByType<NoteObject>(FindObjectsSortMode.None).Length;

        // ✅ Hide results at start
        resultsScreen.SetActive(false);

        // Ensure the music won't loop forever
        audioSource.loop = false;
    }

    void Update()
    {
        if(!_startingPoint && Input.anyKeyDown)
        {
            _startingPoint = true;
            theBs.hasStarted = true;
            audioSource.Play();
            return;
        }

        // ✅ Only show results when song ends, and only once
        if(_resultsShown || audioSource.isPlaying) return;
        ShowResults();
        _resultsShown = true;
    }

    void ShowResults()
    {
        resultsScreen.SetActive(true);

        normalHitTxt.text = normalHits.ToString();
        goodHitTxt.text = goodHits.ToString();
        perfectHitTxt.text = perfectHits.ToString();
        missedHitTxt.text = missedHits.ToString();

        var totalHit = normalHits + goodHits + perfectHits;
        var percentHit = (totalNotes > 0) ? (totalHit / totalNotes) * 100f : 0f;
        percentHitTxt.text = percentHit.ToString("F1") + "%";

        // ✅ Rank calculation

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
    }

    // --- Scoring System ---
    void NoteHit()
    {
        Debug.Log("Hit on time.");

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
        normalHits++;
    }

    public void GoodHit()
    {
        currentScore += scorePerGoodNote * currentMultiplier;
        NoteHit();
        goodHits++;
    }

    public void PerfectHit()
    {
        currentScore += scorePerPerfectNote * currentMultiplier;
        NoteHit();
        perfectHits++;
    }

    public void NoteMissed()
    {
        Debug.Log("Missed note.");
        currentMultiplier = 1;
        multiplierTracker = 0;
        multiTxt.text = "Multiplier: x" + currentMultiplier;
        missedHits++;
    }
}
