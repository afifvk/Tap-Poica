using System;
using UnityEngine;

public class LightstickInput
{
    public ButtonController button; // drag your button here
    LightStickData _lightStickData;
    int _packetDelay;

    [Flags]
    enum LightStickData
    {
        Tap = 1 << 0,
        ShakeStart = 1 << 1,
        ShakeEnd = 1 << 2
    }

    public void UpdateFromPacket(LightStickPacket data)
    {
        _lightStickData = (LightStickData)data.data;

        if(_lightStickData.HasFlag(LightStickData.Tap))
        {
            // Debug.Log("TAP");
            button.OnTapFromController();
            _lightStickData &= ~LightStickData.Tap; // Clear the flag after processing
        }

        if(_lightStickData.HasFlag(LightStickData.ShakeStart))
        {
            // Debug.Log("HOLD START");
            button.OnHoldStartFromController();
            _lightStickData &= ~LightStickData.ShakeStart; // Clear the flag after processing
        }

        if(_lightStickData.HasFlag(LightStickData.ShakeEnd))
        {
            // Debug.Log("HOLD END");
            button.OnHoldEndFromController();
            _lightStickData &= ~LightStickData.ShakeEnd; // Clear the flag after processing
        }

        _packetDelay = data.delay;
    }
}

public struct LightStickPacket
{
    public int delay; // delay in ms
    public byte data; // lightstick data (tap/shake)
}
