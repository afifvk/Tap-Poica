using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BleConnection : MonoBehaviour
{
    public static BleConnection Instance;
    public Text deviceScanButtonText;

    public Text deviceScanStatusText;

    // public Text errorText;
    public Text textSubscribe;

    bool _isScanningDevices;
    bool _isScanningServices;
    bool _isScanningCharacteristics;
    bool _isSubscribed;
    string _lastError;


}
