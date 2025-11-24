using System;
using UnityEngine;

public class LightstickInputSimulator : MonoBehaviour
{
    public ButtonController button; // drag your button here

    void Update()
    {
        // TAP
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("SIM TAP");
            button.OnTapFromController();
        }

        // HOLD START
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("SIM HOLD_START");
            button.OnHoldStartFromController();
        }

        // HOLD END
        if (Input.GetKeyUp(KeyCode.K))
        {
            Debug.Log("SIM HOLD_END");
            button.OnHoldEndFromController();
        }
    }
}