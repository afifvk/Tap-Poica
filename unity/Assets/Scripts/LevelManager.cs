using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager :MonoBehaviour
{
    public static LevelManager Instance;
    public TMP_Dropdown levelDropdown;
    public TMP_Dropdown difficultyDropdown;
    public Button startButton;

    public Level level;
    public LevelDifficulty difficulty;
    AudioSource _lobbyMusic;
    // public LevelData levelData;

    void Awake()
    {
        if(Instance)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _lobbyMusic = gameObject.AddComponent<AudioSource>();


        DontDestroyOnLoad(gameObject);
        // DontDestroyOnLoad(levelDropdown);
        // DontDestroyOnLoad(difficultyDropdown);
        // DontDestroyOnLoad(startButton);
    }

    void Start()
    {
        levelDropdown.ClearOptions();
        var levelNames = LevelData.LevelRegistry
            .Select(meta => $"{meta.displayName} - {meta.artist}").ToList();
        levelDropdown.AddOptions(levelNames);
        levelDropdown.onValueChanged.AddListener(_ =>
        {
            level = (Level)levelDropdown.value;
            InitDifficultyOptions();
            _lobbyMusic.Stop();
            StartCoroutine(LevelLoader.LoadAudioClip(level, clip =>
            {
                _lobbyMusic.clip = clip;
                _lobbyMusic.loop = true;
                _lobbyMusic.Play();
            }));
        });
        level = (Level)levelDropdown.value;
        StartCoroutine(LevelLoader.LoadAudioClip(level, clip =>
        {
            _lobbyMusic.clip = clip;
            _lobbyMusic.loop = true;
            _lobbyMusic.Play();
        }));

        InitDifficultyOptions();
        difficultyDropdown.onValueChanged.AddListener(_ => difficulty = (LevelDifficulty)difficultyDropdown.value);

        startButton.interactable = BleConnection.Instance.controllerConnected;
    }

    void InitDifficultyOptions()
    {
        difficultyDropdown.ClearOptions();
        var difficultyNames = Enum.GetNames(typeof(LevelDifficulty))
            .Take(LevelData.LevelRegistry[(int)level].difficulties).ToList();
        difficultyDropdown.AddOptions(difficultyNames);
        difficulty = (LevelDifficulty)difficultyDropdown.value;
    }

    void Update()
    {
        if(!startButton) return;
        startButton.interactable = BleConnection.Instance.controllerConnected;
    }

    public void StartLevel()
    {
        if(!BleConnection.Instance.controllerConnected) return;
        _lobbyMusic.Stop();
        SceneManager.LoadScene("MainScene");
        // SceneManager.SetActiveScene(SceneManager.GetSceneByName("MainScene"));
    }
}
