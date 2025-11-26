using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public TMP_Dropdown levelDropdown;
    public TMP_Dropdown difficultyDropdown;
    public LevelData levelData;

    void Start()
    {
        levelDropdown.ClearOptions();
        var levelNames = LevelData.LevelRegistry.Select(levelMeta => levelMeta.displayName).ToList();
        levelDropdown.AddOptions(levelNames);
        levelDropdown.onValueChanged.AddListener(_ => levelData.level = (Level)levelDropdown.value);

        difficultyDropdown.ClearOptions();
        var difficultyNames = Enum.GetNames(typeof(LevelDifficulty)).ToList();
        difficultyDropdown.AddOptions(difficultyNames);
        levelDropdown.onValueChanged.AddListener(_ => levelData.difficulty = (LevelDifficulty)difficultyDropdown.value);

        levelData.level = (Level)levelDropdown.value;
        levelData.difficulty = (LevelDifficulty)difficultyDropdown.value;
    }

    public void StartLevel()
    {
        SceneManager.LoadScene("MainScene");
        // SceneManager.SetActiveScene(SceneManager.GetSceneByName("MainScene"));
    }
}
