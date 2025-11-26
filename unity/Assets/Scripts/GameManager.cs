using System;
using System.Collections.Generic;
using System.Globalization;
using OsuParser;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
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
    bool _isHoldingNote = false;

    NoteSpawner _noteSpawner;
    LevelLoader _levelLoader;
    AudioSource _music;

    // Bluetooth stuff
    string _deviceId;
    readonly Dictionary<string, Dictionary<string, string>> _devices = new();
    const string DeviceName = "TapPioca";
    const string ServiceId = "{67676701-6767-6767-6767-676767676767}";
    const string WriteCharacteristicId = "{67676702-6767-6767-6767-676767676767}";
    const string ListenCharacteristicId = "{67676703-6767-6767-6767-676767676767}";
    bool _controllerConnected = false;
    bool _isScanningDevices = false;
    bool _isScanningServices = false;
    bool _isScanningCharacteristics = false;
    string _lastBleError = "Ok";
    LightstickInput _lightstickInput = new();

    void Awake()
    {
        if (Instance != null)
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
        _noteSpawner = gameObject.AddComponent<NoteSpawner>();
        _music = gameObject.AddComponent<AudioSource>();

        scoreTxt.text = "Score: 0";
        currentMultiplier = 1;

        _totalNotes = FindObjectsByType<NoteObject>(FindObjectsSortMode.None).Length;

        _isHoldingNote = false;
        resultsScreen.SetActive(false);
        // nextLevelButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_controllerConnected)
        {
            ConnectController();
            return;
        }

        if (!_levelLoaded) return;

        if (_controllerConnected)
        {
            pollController();
        }

        if (_startingPoint || Input.anyKeyDown) return;
        _startingPoint = true;
        // beatScroller.hasStarted = true;

        if (_isHoldingNote)
        {
            HoldStart();
        }
        else
        {
            HoldEnd();
        }

        if (_resultsShown || _music.isPlaying) return;
        ShowResults();
        _resultsShown = true;
    }

    void ConnectController()
    {
        if (!_isScanningDevices)
        {
            StartStopDeviceScan();
        }

        if (_isScanningDevices)
        {
            ScanDevices();
        }

        if (_isScanningServices)
        {
            ScanServices();
        }

        if (_isScanningCharacteristics)
        {
            ScanCharacteristics();
        }

        {
            // log potential errors
            BleApi.GetError(out var res);
            if (_lastBleError == res.msg) return;
            Debug.LogError("BleApi error: " + res.msg);
            _lastBleError = res.msg;
        }
    }

    void OnApplicationQuit()
    {
        BleApi.Quit();
    }

    void StartStopDeviceScan()
    {
        if (!_isScanningDevices)
        {
            // start new scan
            _devices.Clear();
            BleApi.StartDeviceScan();
            _isScanningDevices = true;
            Debug.Log("Scanning for devices...");
        }
        else
        {
            // stop scan
            _isScanningDevices = false;
            BleApi.StopDeviceScan();
            Debug.Log("Stopped scanning for devices.");
        }
    }

    void ScanDevices()
    {
        var res = new BleApi.DeviceUpdate();

        while (true)
        {
            // Non-blocking poll
            var status = BleApi.PollDevice(ref res, false);

            if (status == BleApi.ScanStatus.Finished)
            {
                _isScanningDevices = false;
                Debug.Log("Failed to find device.");
                StartStopDeviceScan();
            }

            if (status != BleApi.ScanStatus.Available)
            {
                break;
            }

            if (!_devices.ContainsKey(res.id))
                _devices[res.id] = new Dictionary<string, string>()
                {
                    { "name", "" },
                    { "isConnectable", "False" }
                };
            if (res.nameUpdated)
                _devices[res.id]["name"] = res.name;
            if (res.isConnectableUpdated)
                _devices[res.id]["isConnectable"] = res.isConnectable.ToString();

            // Consider only devices which have the right name and which are connectable
            if (_devices[res.id]["name"] != DeviceName || _devices[res.id]["isConnectable"] != "True") continue;
            // This is our device
            StartStopDeviceScan();
            Debug.Log("Connecting to controller...");
            _deviceId = res.id;
            StartServiceScan();
            return;
        }
    }

    void StartServiceScan()
    {
        if (_isScanningServices) return;
        // start new scan
        BleApi.ScanServices(_deviceId);
        _isScanningServices = true;
    }

    void ScanServices()
    {
        while (true)
        {
            var status = BleApi.PollService(out var res, false);

            if (status == BleApi.ScanStatus.Finished)
            {
                _isScanningServices = false;
                Debug.Log("Failed to find service.");
                _isScanningDevices = false;
                StartStopDeviceScan();
            }

            if (status != BleApi.ScanStatus.Available)
            {
                break;
            }

            if (res.uuid != ServiceId) continue;
            // Found our service
            _isScanningServices = false;
            StartCharacteristicScan();
            break;
        }
    }

    void StartCharacteristicScan()
    {
        if (_isScanningCharacteristics) return;
        BleApi.ScanCharacteristics(_deviceId, ServiceId);
        _isScanningCharacteristics = true;
    }

    void ScanCharacteristics()
    {
        while (true)
        {
            var status = BleApi.PollCharacteristic(out var res, false);

            if (status == BleApi.ScanStatus.Finished)
            {
                _isScanningCharacteristics = false;
                Debug.Log("Failed to find characteristic.");
                _isScanningDevices = false;
                StartStopDeviceScan();
            }

            if (status != BleApi.ScanStatus.Available)
            {
                break;
            }

            if (res.uuid != ListenCharacteristicId) continue;
            // Found our characteristic, we are done
            _isScanningCharacteristics = false;
            Subscribe();
            Debug.Log("Controller connected!");
            _levelLoader.Load(levelData.level, levelData.difficulty, OnLevelReady);
            break;
        }
    }

    void Subscribe()
    {
        // no error code available in non-blocking mode
        BleApi.SubscribeCharacteristic(_deviceId, ServiceId, ListenCharacteristicId, false);
        _controllerConnected = true;
    }


    void pollController()
    {
        while (BleApi.PollData(out var res, false))
        {
            Debug.Log("Polling controller...");
            LightStickPacket packet;
            packet.delay = BitConverter.ToInt32(res.buf, 0);
            packet.data = res.buf[4];
            Debug.Log("Delay: " + packet.delay + "us");

            _lightstickInput.UpdateFromPacket(packet);
            _lightstickInput.Update();
        }
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
        _isHoldingNote = false;

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
        // Only hit latest note that can be pressed
        NoteObject latest = null;
        foreach (var n in FindObjectsByType<NoteObject>(FindObjectsSortMode.None))
        {
            if (!n.CanBePressed() || n.noteType != NoteType.Short) continue;
            if (latest == null || n._lifetimeMs < latest._lifetimeMs)
            {
                latest = n;
            }
        }

        if (!latest) return;
        latest.Pressed();
    }

    public void HoldStart()
    {
        _isHoldingNote = true;
        foreach (var n in FindObjectsByType<NoteObject>(FindObjectsSortMode.None))
        {
            // Start all holds that can be started
            if (n._lifetimeMs > 0
                || n.noteType != NoteType.Long
                || n.isBeingHeld) continue;
            n.HoldStart();
        }
    }

    public void HoldEnd()
    {
        _isHoldingNote = false;
        foreach (var n in FindObjectsByType<NoteObject>(FindObjectsSortMode.None))
        {
            // End all held notes
            if (n._lifetimeMs > 0
                || !n.isBeingHeld
                || n.noteType != NoteType.Long) continue;
            n.HoldEnd();
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
