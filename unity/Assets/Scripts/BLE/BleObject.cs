using System;
using UnityEngine;

namespace BLE
{
    /// <summary>
    /// The JSON information that gets send from the Java library
    /// </summary>
    [Serializable]
    public class BleObject
    {
        #region Device Information
        public string Device => device;
        [SerializeField] string device;

        public string Name => name;
        [SerializeField] string name;

        public string Service => service;
        [SerializeField] string service;

        public string Characteristic => characteristic;
        [SerializeField] string characteristic;
        #endregion

        #region Command Information
        public string Command => command;
        [SerializeField] string command;
        #endregion

        #region Error Information
        public bool HasError { get => hasError; }
        [SerializeField] bool hasError;

        public string ErrorMessage { get => errorMessage; }
        [SerializeField] string errorMessage = string.Empty;
        #endregion

        public string Base64Message => base64Message;

        [SerializeField] string base64Message = string.Empty;

        public byte[] GetByteMessage() => Convert.FromBase64String(base64Message);

        public override string ToString() => JsonUtility.ToJson(this, true);
    }
}