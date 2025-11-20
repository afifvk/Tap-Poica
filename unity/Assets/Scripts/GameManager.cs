using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Audio & Gameplay")]
    public AudioSource theMusic;
    public BeatScroller theBS;
    private bool startingPoint;
    private bool resultsShown = false;

    [Header("Score Settings")]
    public int currentScore;
    public int scorePerNote = 100;
    public int scorePerGoodNote = 125;
    public int scorePerPerfectNote = 150;

    [Header("Multiplier Settings")]
    public int currentMultiplier;
    public int multiplierTracker;
    public int[] multiplierThresholds;

    [Header("UI Elements")]
    public Text scoreTxt;
    public Text multiTxt;

    [Header("Results UI")]
    public GameObject resultsScreen;
    public Text percentHitTxt;
    public Text normalHitTxt;
    public Text goodHitTxt;
    public Text perfectHitTxt;
    public Text missedHitTxt;
    public Text rankTxt;
    public Text finalScoreText;

    [Header("Stats Tracking")]
    public float totalNotes;
    public float normalHits;
    public float goodHits;
    public float perfectHits;
    public float missedHits;

    void Start()
    {
        instance = this;

        scoreTxt.text = "Score: 0";
        currentMultiplier = 1;

        totalNotes = FindObjectsOfType<NoteObject>().Length;

        // ✅ Hide results at start
        resultsScreen.SetActive(false);

        // Ensure the music won't loop forever
        theMusic.loop = false;
    }

    void Update()
    {
        if (!startingPoint)
        {
            if (Input.anyKeyDown)
            {
                startingPoint = true;
                theBS.hasStarted = true;
                theMusic.Play();
            }
        }
        else
        {
            // ✅ Only show results when song ends, and only once
            if (!resultsShown && !theMusic.isPlaying)
            {
                ShowResults();
                resultsShown = true;
            }
        }
    }

    void ShowResults()
    {
        resultsScreen.SetActive(true);

        normalHitTxt.text = normalHits.ToString();
        goodHitTxt.text = goodHits.ToString();
        perfectHitTxt.text = perfectHits.ToString();
        missedHitTxt.text = missedHits.ToString();

        float totalHit = normalHits + goodHits + perfectHits;
        float percentHit = (totalNotes > 0) ? (totalHit / totalNotes) * 100f : 0f;
        percentHitTxt.text = percentHit.ToString("F1") + "%";

        // ✅ Rank calculation
        string rankVal = "F";
        if (percentHit > 95) rankVal = "S";
        else if (percentHit > 85) rankVal = "A";
        else if (percentHit > 70) rankVal = "B";
        else if (percentHit > 55) rankVal = "C";
        else if (percentHit > 40) rankVal = "D";
        rankTxt.text = rankVal;

        finalScoreText.text = currentScore.ToString();
    }

    // --- Scoring System ---
    public void NoteHit()
    {
        Debug.Log("Hit on time.");

        if (currentMultiplier - 1 < multiplierThresholds.Length)
        {
            multiplierTracker++;

            if (multiplierThresholds[currentMultiplier - 1] <= multiplierTracker)
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
