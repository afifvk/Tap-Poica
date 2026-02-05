using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager :MonoBehaviour
{
    public TMP_Dropdown levelDropdown;
    public TMP_Dropdown difficultyDropdown;
    public Button startButton;

    public Level level;
    public LevelDifficulty difficulty;
    public AudioSource music;

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
            music.Stop();
            StartMusic();
        });
        level = (Level)levelDropdown.value;
        StartMusic();

        InitDifficultyOptions();
        difficultyDropdown.onValueChanged.AddListener(_ => difficulty = (LevelDifficulty)difficultyDropdown.value);

        startButton.interactable = BleConnection.Instance.controllerConnected;
    }

    void StartMusic()
    {
        StartCoroutine(LevelLoader.LoadAudioClip(level, clip =>
        {
            music.clip = clip;
            music.loop = true;
            music.Play();
        }));
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
}
