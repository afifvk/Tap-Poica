using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace OsuParser
{
    public sealed class OsuBeatmap
    {
        public float sliderMultiplier = 1.4f;
        public double globalBpm;
        public AudioClip audioClip;
        public readonly List<HitObject> hitObjects = new();
    }

    public static class FileParser
    {
        public static OsuBeatmap Parse(string fileContent)
        {
            var data = new OsuBeatmap();

            var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var currentSection = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if(string.IsNullOrWhiteSpace(trimmed)) continue;

                // check for section headers like [General], [TimingPoints], etc.
                if(trimmed.StartsWith("["))
                {
                    currentSection = trimmed;
                    continue;
                }

                switch (currentSection)
                {
                    case "[Difficulty]":
                    {
                        if(!trimmed.StartsWith("SliderMultiplier:")) break;
                        data.sliderMultiplier = float.Parse(trimmed.Split(':')[1], CultureInfo.InvariantCulture);
                        break;
                    }
                    case "[TimingPoints]":
                    {
                        if(data.globalBpm != 0) break;
                        ParseBpm(trimmed, data);
                        break;
                    }
                    case "[HitObjects]":
                    {
                        var obj = ParseLine(trimmed);
                        if(obj != null) data.hitObjects.Add(obj);
                        break;
                    }
                }
            }

            return data;
        }

        static void ParseBpm(string line, OsuBeatmap data)
        {
            // format: time,beatLength,meter,sampleSet,sampleIndex,volume,uninherited,effects
            // example: 753,444.444,4,2,1,85,1,0

            var parts = line.Split(',');

            // beatLength is the 2nd value (index 1)
            var beatLength = double.Parse(parts[1], CultureInfo.InvariantCulture);

            // uninherited flag is the 7th value (index 6). 1 = true, 0 = false.
            // if it's 0 (green line), it's just a speed change, not a BPM change.
            var isUninherited = true;

            if(parts.Length > 6)
            {
                isUninherited = parts[6] == "1";
            }

            if(isUninherited && beatLength > 0)
            {
                // math: bpm = 60000 / ms_per_beat
                data.globalBpm = 60000.0 / beatLength;
            }
        }

        // This is the method we will fill out in the next step
        static HitObject ParseLine(string line)
        {
            var parts = line.Split(',');

            var x = int.Parse(parts[0]);
            var y = int.Parse(parts[1]);
            var time = int.Parse(parts[2]);
            var type = (HitObjectType)int.Parse(parts[3]);
            var hitSound = int.Parse(parts[4]);

            if(type.HasFlag(HitObjectType.Circle))
                return new HitCircle
                {
                    X = x,
                    Y = y,
                    Time = time,
                    Type = type,
                    HitSound = hitSound,
                    HitSample = parts.Length > 5 ? parts[5] : ""
                };

            if(type.HasFlag(HitObjectType.Spinner))
                return new Spinner
                {
                    X = x,
                    Y = y,
                    Time = time,
                    Type = type,
                    HitSound = hitSound,
                    EndTime = int.Parse(parts[5]),
                    HitSample = parts.Length > 6 ? parts[6] : ""
                };

            if(type.HasFlag(HitObjectType.Slider))
                return ParseSlider(parts, x, y, time, type, hitSound);

            if(!type.HasFlag(HitObjectType.ManiaHold)) return null;

            var sampleParts = parts[5].Split(':');
            return new ManiaHold()
            {
                X = x,
                Y = y,
                Time = time,
                Type = type,
                HitSound = hitSound,
                EndTime = int.Parse(sampleParts[0]),
                HitSample = sampleParts[1],
            };
        }

        static Slider ParseSlider(string[] parts, int x, int y, int time, HitObjectType type, int hitSound)
        {
            // format: x,y,time,type,hitSound,curveType|curvePoints,slides,length,edgeSounds,edgeSets,hitSample

            // parts[5] looks like "B|200:200|250:250"
            var curveData = parts[5].Split('|');
            var curveType = curveData[0][0]; // First char of first item

            // The rest are points. We skip the first one because that was the type
            var curvePoints = new List<Vector2>();

            // Start loop at 1 because index 0 is the curve type (e.g., "P")
            for (var i = 1; i < curveData.Length; i++)
            {
                var pointParts = curveData[i].Split(':');
                var px = float.Parse(pointParts[0]);
                var py = float.Parse(pointParts[1]);

                curvePoints.Add(new Vector2(px, py));
            }

            var slides = int.Parse(parts[6]);

            // This is the "absolute length" you wanted
            // It is the visual length in osu! pixels
            var length = double.Parse(parts[7]);

            return new Slider
            {
                X = x,
                Y = y,
                Time = time,
                Type = type,
                HitSound = hitSound,
                CurveType = curveType,
                CurvePoints = curvePoints,
                Slides = slides,
                Length = length,
                HitSample = parts.Length > 10 ? parts[10] : ""
            };
        }
    }
}
