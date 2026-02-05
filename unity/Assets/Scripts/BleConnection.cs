using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BleConnection :MonoBehaviour
{
    public static BleConnection Instance;
    public Text statusText;

    const string DeviceName = "TapPioca";
    const string ServiceId = "{67676701-6767-6767-6767-676767676767}";
    // const string WriteCharacteristicId = "{67676702-6767-6767-6767-676767676767}";
    const string ListenCharacteristicId = "{67676703-6767-6767-6767-676767676767}";

    readonly Dictionary<string, Dictionary<string, string>> _devices = new();
    string _deviceId;
    bool _isScanningDevices;
    bool _isScanningServices;
    bool _isScanningCharacteristics;
    bool _isSubscribed;
    // string _lastBleError = "Ok";
    [HideInInspector] public bool controllerConnected;

    void Start()
    {
        Instance = this;
        ConnectController();
    }

    void Update()
    {
        if(controllerConnected) return;
        ConnectController();
    }

    void ConnectController()
    {
        if(!_isScanningDevices)
        {
            StartStopDeviceScan();
        }

        if(_isScanningDevices)
        {
            ScanDevices();
        }

        if(_isScanningServices)
        {
            ScanServices();
        }

        if(_isScanningCharacteristics)
        {
            ScanCharacteristics();
        }

        // log potential errors
        // BleApi.GetError(out var res);
        // if(_lastBleError == res.msg) return;
        // Debug.LogError("BleApi error: " + res.msg);
        // _lastBleError = res.msg;
    }

    void OnApplicationQuit()
    {
        BleApi.Quit();
    }

    void StartStopDeviceScan()
    {
        if(!_isScanningDevices)
        {
            // start new scan
            _devices.Clear();
            BleApi.StartDeviceScan();
            _isScanningDevices = true;
            statusText.text = "Searching for controller...";
            // Debug.Log("Scanning for devices...");
        }
        else
        {
            // stop scan
            _isScanningDevices = false;
            BleApi.StopDeviceScan();
            // Debug.Log("Stopped scanning for devices.");
        }
    }

    void ScanDevices()
    {
        var res = new BleApi.DeviceUpdate();

        while (true)
        {
            // Non-blocking poll
            var status = BleApi.PollDevice(ref res, false);

            if(status == BleApi.ScanStatus.Finished)
            {
                _isScanningDevices = false;
                statusText.text = "Failed to find controller";
                // Debug.Log("Failed to find device.");
                StartStopDeviceScan();
            }

            if(status != BleApi.ScanStatus.Available)
            {
                break;
            }

            if(!_devices.ContainsKey(res.id))
                _devices[res.id] = new Dictionary<string, string>()
                {
                    { "name", "" },
                    { "isConnectable", "False" }
                };
            if(res.nameUpdated)
                _devices[res.id]["name"] = res.name;
            if(res.isConnectableUpdated)
                _devices[res.id]["isConnectable"] = res.isConnectable.ToString();
            if (res.name != "") {
                Debug.Log("Found name: " + res.name + " (" + res.isConnectable.ToString() + ")");
            }

            // Consider only devices which have the right name
            // Sometimes the tinyscreen never adverts itself as connectable and this connects faster
            // Even if it's not connectable, we can try to connect and it'll work anyway 💀
            if (_devices[res.id]["name"] != DeviceName) continue;
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
        if(_isScanningServices) return;
        // start new scan
        BleApi.ScanServices(_deviceId);
        _isScanningServices = true;
    }

    void ScanServices()
    {
        while (true)
        {
            var status = BleApi.PollService(out var res, false);

            if(status == BleApi.ScanStatus.Finished)
            {
                _isScanningServices = false;
                _isScanningDevices = false;
                StartStopDeviceScan();
            }

            if(status != BleApi.ScanStatus.Available)
            {
                break;
            }

            if(res.uuid != ServiceId) continue;
            // Found our service
            _isScanningServices = false;
            StartCharacteristicScan();
            break;
        }
    }

    void StartCharacteristicScan()
    {
        if(_isScanningCharacteristics) return;
        BleApi.ScanCharacteristics(_deviceId, ServiceId);
        _isScanningCharacteristics = true;
    }

    void ScanCharacteristics()
    {
        while (true)
        {
            var status = BleApi.PollCharacteristic(out var res, false);

            if(status == BleApi.ScanStatus.Finished)
            {
                _isScanningCharacteristics = false;
                Debug.LogError("Failed to find characteristic.");
                _isScanningDevices = false;
                StartStopDeviceScan();
            }

            if(status != BleApi.ScanStatus.Available)
            {
                break;
            }

            if(res.uuid != ListenCharacteristicId) continue;
            // Found our characteristic, we are done
            _isScanningCharacteristics = false;
            Subscribe();
            statusText.text = "Controller connected!";
            Debug.Log("Controller connected!");
            break;
        }
    }

    void Subscribe()
    {
        // statusText.text = "Found controller!";
        // no error code available in non-blocking mode
        BleApi.SubscribeCharacteristic(_deviceId, ServiceId, ListenCharacteristicId, false);
        controllerConnected = true;
    }
}
