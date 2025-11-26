using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BleConnection :MonoBehaviour
{
    public static BleConnection Instance;
    public Text deviceScanButtonText;

    public Text deviceScanStatusText;

    // public Text errorText;
    public Text textSubscribe;

    string _deviceId;
    readonly Dictionary<string, Dictionary<string, string>> _devices = new();
    const string DeviceName = "TapPioca";
    const string ServiceId = "{67676701-6767-6767-6767-676767676767}";
    const string WriteCharacteristicId = "{67676702-6767-6767-6767-676767676767}";
    const string ListenCharacteristicId = "{67676703-6767-6767-6767-676767676767}";

    bool _isScanningDevices;
    bool _isScanningServices;
    bool _isScanningCharacteristics;
    bool _isSubscribed;
    string _lastError;

    void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
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

        if(_isSubscribed)
        {
            while (BleApi.PollData(out var res, false))
            {
                // textSubscribe.text = BitConverter.ToString(res.buf, 0, res.size);
                // TextSubscribe.text = Encoding.ASCII.GetString(res.buf, 0, res.size);
            }
        }

        {
            // log potential errors
            BleApi.GetError(out var res);
            if(_lastError == res.msg) return;
            Debug.LogError("BleApi error: " + res.msg);
            _lastError = res.msg;
        }
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
            deviceScanButtonText.text = "Stop scan";
            deviceScanStatusText.text = "scanning";
        }
        else
        {
            // stop scan
            _isScanningDevices = false;
            BleApi.StopDeviceScan();
            deviceScanButtonText.text = "Start scan";
            deviceScanStatusText.text = "stopped";
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
                deviceScanButtonText.text = "Scan devices";
                deviceScanStatusText.text = "No device";
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

            // Consider only devices which have the right name and which are connectable
            if(_devices[res.id]["name"] != DeviceName || _devices[res.id]["isConnectable"] != "True") continue;
            // This is our device
            StartStopDeviceScan();
            Debug.Log("Connected");
            _deviceId = res.id;
            deviceScanStatusText.text = "connecting...";
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
                deviceScanStatusText.text = "failed";
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
                deviceScanStatusText.text = "failed";
            }

            if(status != BleApi.ScanStatus.Available)
            {
                break;
            }

            if(res.uuid != ListenCharacteristicId) continue;
            // Found our characteristic, we are done
            _isScanningCharacteristics = false;
            Subscribe();
            deviceScanStatusText.text = "connected";
            SceneManager.LoadScene("StartScene");
            // SceneManager.SetActiveScene(SceneManager.GetSceneByName("StartScene"));
            break;
        }
    }

    void Subscribe()
    {
        // no error code available in non-blocking mode
        BleApi.SubscribeCharacteristic(_deviceId, ServiceId, ListenCharacteristicId, false);
        _isSubscribed = true;
    }

    // Message must be ASCII and less than 512 bytes
    public void Write(string message)
    {
        var payload = Encoding.ASCII.GetBytes(message);
        var data = new BleApi.BleData
        {
            buf = new byte[512],
            size = (short)payload.Length,
            deviceId = _deviceId,
            serviceUuid = ServiceId,
            characteristicUuid = ListenCharacteristicId
        };
        for (var i = 0; i < payload.Length; i++)
            data.buf[i] = payload[i];
        // no error code available in non-blocking mode
        BleApi.SendData(in data, false);
    }
}
