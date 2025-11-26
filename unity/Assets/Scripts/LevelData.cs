using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "LevelData", order = 1)]
public class LevelData :ScriptableObject
{
    public Level level;
    public LevelDifficulty difficulty;

    public static readonly List<LevelMetadata> LevelRegistry = new()
    {
        new LevelMetadata
        {
            displayName = "Iris Out",
            artist = "Kenshi Yonezu",
            folderName = "irisout",
            difficulties = 3
        },
        new LevelMetadata
        {
            displayName = "Fancy",
            artist = "TWICE",
            folderName = "fancy",
            difficulties = 3
        },
        new LevelMetadata()
        {
            displayName = "DDU-DU DDU-DU",
            folderName = "ddududdudu",
            artist = "BLACKPINK",
            difficulties = 4
        },
        new LevelMetadata()
        {
            displayName = "How You Like That",
            folderName = "howyoulikethat",
            artist = "BLACKPINK",
            difficulties = 3
        },
        new LevelMetadata()
        {
            displayName = "APT.",
            folderName = "apt",
            artist = "ROSÉ",
            difficulties = 3
        },
        new LevelMetadata()
        {
            displayName = "Kill This Love",
            folderName = "killthislove",
            artist = "BLACKPINK",
            difficulties = 4
        }
    };
}

public enum Level
{
    IrisOut,
    Fancy,
    DduDuDduDu,
    HowYouLikeThat,
    Apt,
}

public enum LevelDifficulty
{
    Easy,
    Medium,
    Hard,
    Expert
}

public class LevelMetadata
{
    public string displayName;
    public string artist;
    public string folderName; // e.g. "irisout"
    public int difficulties;
}
