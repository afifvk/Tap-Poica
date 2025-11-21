using BLE.Commands.Base;
using BLE.Extension;

namespace BLE.Commands
{
    /// <summary>
    /// Command to Subscribe to a BLE Device's Characteristic
    /// </summary>
    public class SubscribeToCharacteristic :BleCommand
    {
        /// <summary>
        /// The UUID of the BLE device.
        /// </summary>
        public readonly string deviceAddress;

        /// <summary>
        /// The Service that parents the Characteristic.
        /// </summary>
        public readonly string service;

        /// <summary>
        /// The Characteristic to write the message to.
        /// </summary>
        public readonly string characteristic;

        /// <summary>
        /// The .NET event that sends the subscribe data back to the user.
        /// </summary>
        public readonly CharacteristicChanged onCharacteristicChanged;

        /// <summary>
        /// Indicates if the UUID is custom (long-uuid instead of a short-hand).
        /// </summary>
        readonly bool _customGatt;

        /// <summary>
        /// Subscribes to a given BLE Characteristic.
        /// </summary>
        /// <param name="deviceAddress">The UUID of the device that the BLE should subscribe to.</param>
        /// <param name="service">The UUID of the Service that parents the Characteristic.</param>
        /// <param name="characteristic">The UUID of the Characteristic to read from.</param>
        /// <param name="customGatt"><see langword="true"/> if the GATT Characteristic UUID address is a long-hand, not short-hand.</param>
        public SubscribeToCharacteristic(string deviceAddress, string service, string characteristic,
            bool customGatt = false) :base(true, true)
        {
            this.deviceAddress = deviceAddress;

            this.service = service;
            this.characteristic = characteristic;

            _customGatt = customGatt;
        }

        /// <summary>
        /// Subscribes to a given BLE Characteristic and passes the data back to the user.
        /// </summary>
        /// <param name="deviceAddress">The UUID of the device that the BLE should subscribe to.</param>
        /// <param name="service">The UUID of the Service that parents the Characteristic.</param>
        /// <param name="characteristic">The UUID of the Characteristic to read from.</param>
        /// <param name="onDataFound">The <see cref="CharacteristicChanged"/> that will trigger if a value was updated on the Characteristic.</param>
        /// <param name="customGatt"><see langword="true"/> if the GATT Characteristic UUID address is a long-hand, not short-hand.</param>
        public SubscribeToCharacteristic(string deviceAddress, string service, string characteristic,
            CharacteristicChanged onDataFound, bool customGatt = false) :base(true, true)
        {
            this.deviceAddress = deviceAddress;

            this.service = service;
            this.characteristic = characteristic;

            onCharacteristicChanged += onDataFound;

            _customGatt = customGatt;
        }

        public override void Start()
        {
            var command = _customGatt ? "subscribeToCustomGattCharacteristic" : "subscribeToGattCharacteristic";
            BleManager.SendCommand(command, deviceAddress, service, characteristic);
        }

        public override void End()
        {
            var command = _customGatt ? "unsubscribeFromCustomGattCharacteristic" : "unsubscribeFromGattCharacteristic";
            BleManager.SendCommand(command, deviceAddress, service, characteristic);
        }

        public void Unsubscribe() => End();

        public override bool CommandReceived(BleObject obj)
        {
            if(!string.Equals(obj.Command, "CharacteristicValueChanged")) return false;

            string objService, objCharacteristic;

            if(_customGatt)
            {
                objService = obj.Service;
                objCharacteristic = obj.Characteristic;
            }
            else
            {
                objService = obj.Service.Get4BitUuid();
                objCharacteristic = obj.Characteristic.Get4BitUuid();
            }

            if(string.Equals(obj.Device, deviceAddress) &&
               string.Equals(objService, service) &&
               string.Equals(objCharacteristic, characteristic))
            {
                onCharacteristicChanged?.Invoke(obj.GetByteMessage());
            }

            return false;
        }

        /// <summary>
        /// A delegate that indicates a newly updated value on a Characteristic.
        /// </summary>
        /// <param name="value">The value that was updated on the Characteristic.</param>
        public delegate void CharacteristicChanged(byte[] value);
    }
}
