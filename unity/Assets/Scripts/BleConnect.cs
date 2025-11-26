using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Demo : MonoBehaviour
{
    public Text deviceScanButtonText;
    public Text deviceScanStatusText;
    public Text errorText;
    public Text TextSubscribe;

    public string deviceId;
    public readonly string deviceName = "TapPioca";
    public readonly string serviceId = "67676767-6702-6767-6767-676767676767";
    public readonly string characteristicId = "67676767-6703-6767-6767-676767676767";

    bool isScanningDevices = false;
    bool isScanningServices = false;
    bool isScanningCharacteristics = false;
    bool isSubscribed = false;
    string lastError;

    // Start is called before the first frame update
    void Start()
    { }

    // Update is called once per frame
    void Update()
    {
        if (isScanningDevices)
        {
            ScanDevices();
        }
        if (isScanningServices)
        {
            ScanServices();
        }
        if (isScanningCharacteristics)
        {
            ScanCharacteristics();
        }
        if (isSubscribed)
        {
            BleApi.BLEData res = new BleApi.BLEData();
            while (BleApi.PollData(out res, false))
            {
                TextSubscribe.text = BitConverter.ToString(res.buf, 0, res.size);
                // TextSubscribe.text = Encoding.ASCII.GetString(res.buf, 0, res.size);
            }
        }

        {
            // log potential errors
            BleApi.ErrorMessage res = new BleApi.ErrorMessage();
            BleApi.GetError(out res);
            if (lastError != res.msg)
            {
                Debug.LogError("BleApi error: " + res.msg);
                lastError = res.msg;
            }
        }
    }

    private void OnApplicationQuit()
    {
        BleApi.Quit();
    }

    public void StartStopDeviceScan()
    {
        if (!isScanningDevices)
        {
            // start new scan
            BleApi.StartDeviceScan();
            isScanningDevices = true;
            deviceScanButtonText.text = "Stop scan";
            deviceScanStatusText.text = "scanning";
        }
        else
        {
            // stop scan
            isScanningDevices = false;
            BleApi.StopDeviceScan();
            deviceScanButtonText.text = "Start scan";
            deviceScanStatusText.text = "stopped";
        }
    }

    private void ScanDevices()
    {
        BleApi.DeviceUpdate res = new BleApi.DeviceUpdate();
        while (true)
        {
            // Non-blocking poll
            BleApi.ScanStatus status = BleApi.PollDevice(ref res, false);
            if (status == BleApi.ScanStatus.FINISHED)
            {
                isScanningDevices = false;
                deviceScanButtonText.text = "Scan devices";
                deviceScanStatusText.text = "No device";
            }
            if (status != BleApi.ScanStatus.AVAILABLE)
            {
                break;
            }

            // Consider only devices which have the right name and which are connectable
            if (res.name == deviceName && res.isConnectable.ToString() == "True")
            {
                // This is our device
                deviceId = res.id;
                StartStopDeviceScan();
                deviceScanStatusText.text = "connecting...";
                StartServiceScan();
                break;
            }
        }
    }

    public void StartServiceScan()
    {
        if (!isScanningServices)
        {
            // start new scan
            BleApi.ScanServices(deviceId);
            isScanningServices = true;
        }
    }

    private void ScanServices()
    {
        BleApi.Service res = new BleApi.Service();
        while (true)
        {
            BleApi.ScanStatus status = BleApi.PollService(out res, false);
            if (status == BleApi.ScanStatus.FINISHED)
            {
                isScanningServices = false;
                deviceScanStatusText.text = "failed";
            }
            if (status != BleApi.ScanStatus.AVAILABLE)
            {
                break;
            }

            if (res.uuid == serviceId)
            {
                // Found our service
                isScanningServices = false;
                StartCharacteristicScan();
                break;
            }
        }
    }

    public void StartCharacteristicScan()
    {
        if (!isScanningCharacteristics)
        {
            BleApi.ScanCharacteristics(deviceId, serviceId);
            isScanningCharacteristics = true;
        }
    }

    private void ScanCharacteristics()
    {
        BleApi.Characteristic res = new BleApi.Characteristic();
        while (true)
        {
            BleApi.ScanStatus status = BleApi.PollCharacteristic(out res, false);
            if (status == BleApi.ScanStatus.FINISHED)
            {
                isScanningCharacteristics = false;
                deviceScanStatusText.text = "failed";
            }
            if (status != BleApi.ScanStatus.AVAILABLE)
            {
                break;
            }

            if (res.uuid == characteristicId)
            {
                // Found our characteristic, we are done
                isScanningCharacteristics = false;
                Subscribe();
                deviceScanStatusText.text = "connected";
                SceneManager.LoadScene("StartScene(1)");
                break;
            }
        }
    }

    public void Subscribe()
    {
        // no error code available in non-blocking mode
        BleApi.SubscribeCharacteristic(deviceId, serviceId, characteristicId, false);
        isSubscribed = true;
    }

    // Message must be ASCII and less than 512 bytes
    public void Write(string message)
    {
        byte[] payload = Encoding.ASCII.GetBytes(message);
        BleApi.BLEData data = new BleApi.BLEData();
        data.buf = new byte[512];
        data.size = (short)payload.Length;
        data.deviceId = deviceId;
        data.serviceUuid = serviceId;
        data.characteristicUuid = characteristicId;
        for (int i = 0; i < payload.Length; i++)
            data.buf[i] = payload[i];
        // no error code available in non-blocking mode
        BleApi.SendData(in data, false);
    }
}
