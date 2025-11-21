using BLE.Commands.Base;
using BLE.Extension;

namespace BLE.Commands
{
    /// <summary>
    /// Command to read from a given Characteristic.
    /// </summary>
    public class ReadFromCharacteristic :BleCommand
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
        /// The .NET event that sends the read data back to the user.
        /// </summary>
        public ReadCharacteristicValueReceived onReadCharacteristicValueReceived;

        /// <summary>
        /// Indicates if the UUID is custom (long-uuid instead of a short-hand).
        /// </summary>
        public readonly bool customGatt;

        /// <summary>
        /// Reads from a given BLE Characteristic.
        /// </summary>
        /// <param name="deviceAddress">The UUID of the device that the BLE should read from.</param>
        /// <param name="serviceAddress">The UUID of the Service that parents the Characteristic.</param>
        /// <param name="characteristicAddress">The UUID of the Characteristic to read from.</param>
        /// <param name="valueReceived">The <see cref="ReadCharacteristicValueReceived"/> that will trigger if a value was read from the Characteristic.</param>
        /// <param name="customGatt"><see langword="true"/> if the GATT Characteristic UUID address is a long-hand, not short-hand.</param>
        public ReadFromCharacteristic(string deviceAddress, string serviceAddress, string characteristicAddress,
            ReadCharacteristicValueReceived valueReceived, bool customGatt = false)
        {
            this.deviceAddress = deviceAddress;
            service = serviceAddress;
            characteristic = characteristicAddress;

            onReadCharacteristicValueReceived = valueReceived;

            this.customGatt = customGatt;

            timeout = 1f;
        }

        public override void Start()
        {
            var command = customGatt ? "readFromCustomCharacteristic" : "readFromCharacteristic";
            BleManager.SendCommand(command, deviceAddress, service, characteristic);
        }

        public override bool CommandReceived(BleObject obj)
        {
            if(!string.Equals(obj.Command, "ReadFromCharacteristic")) return false;
            if((customGatt || !string.Equals(obj.Characteristic.Get4BitUuid(), characteristic) ||
                !string.Equals(obj.Service.Get4BitUuid(), service))
               && (!customGatt || !string.Equals(obj.Characteristic, characteristic) ||
                   !string.Equals(obj.Service, service))) return false;
            onReadCharacteristicValueReceived?.Invoke(obj.GetByteMessage());
            return true;

        }

        /// <summary>
        /// A delegate that indicates a read value from a Characteristic.
        /// </summary>
        /// <param name="value">The value that was read from the Characteristic.</param>
        public delegate void ReadCharacteristicValueReceived(byte[] value);
    }
}
