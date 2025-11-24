using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Header("Next Level Button")]
    public Button nextLevelButton;

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

        resultsScreen.SetActive(false);
        nextLevelButton.gameObject.SetActive(false);
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

        string rankVal = "F";
        if (percentHit > 95) rankVal = "S";
        else if (percentHit > 85) rankVal = "A";
        else if (percentHit > 70) rankVal = "B";
        else if (percentHit > 55) rankVal = "C";
        else if (percentHit > 40) rankVal = "D";
        rankTxt.text = rankVal;

        finalScoreText.text = currentScore.ToString();

        nextLevelButton.gameObject.SetActive(true);
    }

    // --- Hit & Hold Notes Programmatically ---
    public void HitNote()
    {
        NoteObject[] notes = FindObjectsOfType<NoteObject>();
        NoteObject closest = null;
        float bestDist = Mathf.Infinity;

        foreach (var n in notes)
        {
            if (n.canBePressed)
            {
                float d = Mathf.Abs(n.transform.position.y);
                if (d < bestDist)
                {
                    bestDist = d;
                    closest = n;
                }
            }
        }

        if (closest != null)
            closest.Pressed();
    }

    public void HoldStart()
    {
        foreach (var n in FindObjectsOfType<NoteObject>())
        {
            if (n.canBePressed && n.isLongNote)
            {
                n.HoldStart();
                return;
            }
        }
    }

    public void HoldEnd()
    {
        foreach (var n in FindObjectsOfType<NoteObject>())
        {
            if (n.isBeingHeld)
            {
                n.HoldEnd();
                return;
            }
        }
    }

    // --- Scoring System ---
    public void NoteHit()
    {
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
        currentMultiplier = 1;
        multiplierTracker = 0;
        multiTxt.text = "Multiplier: x" + currentMultiplier;
        missedHits++;
    }

    // ----------------------------------------------------------
    // NEXT LEVEL BUTTON → Load next scene
    // ----------------------------------------------------------
    public void LoadNextLevel()
    {
        Time.timeScale = 1f;

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.Log("Reached the last level!");
        }
    }

    // ----------------------------------------------------------
    // EXIT LAST LEVEL → Load specific scene (e.g. Main Menu)
    // ----------------------------------------------------------
    public void ExitToScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
