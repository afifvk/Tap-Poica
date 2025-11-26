using System.Collections.Generic;

namespace OsuParser
{
    public static class NoteConverter
    {
        const float BaseScoringDistance = 100f;

        public static List<NoteData> Convert(OsuBeatmap osuBeatmap)
        {
            var gameNotes = new List<NoteData>();
            var beatLength = osuBeatmap.globalBpm > 0 ? (60000.0 / osuBeatmap.globalBpm) : 500.0;

            foreach (var obj in osuBeatmap.hitObjects)
            {
                switch (obj)
                {
                    case HitCircle:
                        gameNotes.Add(new NoteData
                        {
                            timeMs = obj.Time,
                            type = NoteType.Short,
                            durationMs = 0
                        });
                        break;
                    case Spinner spin:
                        gameNotes.Add(new NoteData
                        {
                            timeMs = spin.Time,
                            type = NoteType.Long,
                            durationMs = spin.EndTime - spin.Time
                        });
                        break;
                    case ManiaHold hold:
                        gameNotes.Add(new NoteData
                        {
                            timeMs = hold.Time,
                            type = NoteType.Long,
                            durationMs = hold.EndTime - hold.Time
                        });
                        break;
                    case Slider slider:
                    {
                        var singleSlideDuration =
                            (slider.Length / BaseScoringDistance * osuBeatmap.sliderMultiplier) * beatLength;

                        gameNotes.Add(new NoteData
                        {
                            timeMs = slider.Time,
                            type = NoteType.Long,
                            durationMs = singleSlideDuration * slider.Slides
                        });

                        for (var i = 1; i < slider.Slides; i++)
                            gameNotes.Add(new NoteData
                            {
                                timeMs = slider.Time + (singleSlideDuration * i),
                                type = NoteType.Short
                            });

                        break;
                    }

                }
            }

            gameNotes.Sort((a, b) => a.timeMs.CompareTo(b.timeMs));
            return gameNotes;
        }
    }
}
