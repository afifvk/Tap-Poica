#include "BMA250.h"
#include <STBLE.h>
#include <Wire.h>

#include "DisplayManager.h"
#include "SampleBuffer.h"

#define TAP_THRESH 5600.0
#define TAP_DEBOUNCE 60000 // microseconds
#define SHAKE_THRESH 510.0
#define SHAKE_DEBOUNCE 130000 // microseconds

#define VIBRATE_PIN 6
#define VIBRATE_PIN_ACTIVE HIGH
#define VIBRATE_PIN_INACTIVE LOW

#define TAP_PIOCA_DEVICE_NAME "TapPioca"
#define TAP_PIOCA_SERVICE_UUID "67676701-6767-6767-6767-676767676767"
#define TAP_PIOCA_CHAR_TX_UUID "67676702-6767-6767-6767-676767676767"

#define FLAG_IDLE 0x00
#define FLAG_TAP 0x01
#define FLAG_SHAKE_START 0x02
#define FLAG_SHAKE_END 0x04

char sprintbuff[100];

void bleOnConnect();
void bleOnDisconnect();
void bleOnBond();
// void packInt32(uint8_t *d, uint32_t val);
// uint32_t unpackInt32(uint8_t *src);

//
// Global Objects
//
TinyScreen display = TinyScreen(TinyScreenDefault);
Timer timer(display);
MenuManager menuManager(timer, display);
BLEManager bleManager(TAP_PIOCA_DEVICE_NAME, TAP_PIOCA_SERVICE_UUID,
                      bleOnConnect, bleOnDisconnect, bleOnBond);
DisplayManager displayManager(display, timer, menuManager, bleManager);
SampleBuffer samples;
BMA250 accel_sensor;
uint32_t clock_micros;
uint32_t last_tap = 0, last_shake = 0;
bool is_shaking = false;
uint8_t packetFlags;
uint32_t lastDetectTime = 0;

// uint32_t currentMicros = 0;
BLEServ uartService;
BLEServ tapPoicaService;
BLEChar tapPoicaTxChar;
BLEChar tapPoicaRxChar;
BLEChar uartTxChar;

void setup() {
  SerialMonitorInterface.begin(115200);
  Wire.begin();
  pinMode(VIBRATE_PIN, OUTPUT);
  digitalWrite(VIBRATE_PIN, VIBRATE_PIN_INACTIVE);

  PRINTF("Initializing BMA...");
  if (accel_sensor.begin(BMA250_range_2g, BMA250_update_time_4ms)) {
    PRINTF("ERROR! NO BMA250 DETECTED!");
  }

  displayManager.begin();
  bleManager.setupServices = bleSetupServices;
  bleManager.begin();
  clock_micros = micros();
  // currentMicros = micros();
}

void loop() {
  // uint32_t elapsed = micros() - clock_micros;
  // lastDetectTime += elapsed;
  clock_micros = micros();

  // if (SAMPLE_MICROS < lastDetectTime) {
  Sample sample = accel_sensor.read();
  samples.append(sample);
  // PRINTF("last detect: %lu\n", lastDetectTime);
  if (clock_micros - last_tap > TAP_DEBOUNCE) {
    packetFlags |= detectTap();
  }
  packetFlags |= detectShake();
  uint8_t lastDetect = micros();

  uint8_t packet[5];

  packInt32(packet, clock_micros);
  packet[4] = packetFlags;
  if (packetFlags) {
    PRINTF("write took: %lu\n", micros() - lastDetect - clock_micros);
    bleWrite((char *)packet, 5);
    packetFlags = 0;
  }

  // lastDetectTime = 0;
  // }

  bleManager.update();
  displayManager.update();

  waitForNextSample();
}

uint8_t detectTap() {
  float val = samples.get(samples.len() - 1) - samples.mean();
  if (val > TAP_THRESH || val < -TAP_THRESH) {
    PRINTF("Tap\n");
    last_tap = clock_micros;
    return FLAG_TAP;
  }
  return FLAG_IDLE;
}

uint8_t detectShake() {
  float val = samples.std_dev();
  bool still_shaking = (val > SHAKE_THRESH);
  if (still_shaking) {
    last_shake = clock_micros;
  }
  if (is_shaking == still_shaking) {
    return FLAG_IDLE;
  }

  if (is_shaking && clock_micros - last_shake > SHAKE_DEBOUNCE) {
    PRINTF("Shake end\n");
    is_shaking = false;
    return FLAG_SHAKE_END;
  } else if (!is_shaking) {
    PRINTF("Shake start\n");
    is_shaking = true;
    return FLAG_SHAKE_START;
  }
  return FLAG_IDLE;
}

void waitForNextSample() {
  // Underflow is fine here as we only take the difference,
  // which is almost guaranteed to not overflow (70mins for a frame is crazy).
  uint32_t elapsed = micros() - clock_micros;

  if (SAMPLE_MICROS > elapsed) {
    uint32_t remaining_milis = (SAMPLE_MICROS - elapsed) / 1000;
    delay(remaining_milis);
  }
  clock_micros = micros();

  // PRINTF("micros: %llu\n", elapsed);
}

void bleOnConnect() {
  PRINTF("---------Connect\n");
  tBleStatus ret = aci_gap_slave_security_request(
      bleManager.phoneConnection.handle, BONDING, MITM_PROTECTION_NOT_REQUIRED);

  if (ret != BLE_STATUS_SUCCESS)
    PRINTF("Slave security request error: %d\n", (uint8_t)ret);
}

void bleOnDisconnect() {
  PRINTF("---------Disconnect\n");
  bleManager.bleConnectionState = false;
  bleManager.advertise();
}

void bleOnBond() { PRINTF("---------Bonded\n"); }

void bleSetupServices() {
  BLEManager::addService(&tapPoicaService, TAP_PIOCA_SERVICE_UUID,
                         PRIMARY_SERVICE, 7);

  BLEManager::addCharacteristic(&tapPoicaService, &tapPoicaTxChar,
                                TAP_PIOCA_CHAR_TX_UUID, 5, CHAR_PROP_NOTIFY,
                                ATTR_PERMISSION_NONE, GATT_DONT_NOTIFY_EVENTS);
}

uint8_t bleWrite(char *payload, uint8_t dataSize) {
  tBleStatus ret =
      aci_gatt_update_char_value(tapPoicaService.handle, tapPoicaTxChar.handle,
                                 0, dataSize, (uint8_t *)payload);

  if (ret != BLE_STATUS_SUCCESS)
    PRINTF("Error while updating characteristic.\n");

  if (ret == BLE_STATUS_INSUFFICIENT_RESOURCES)
    PRINTF("Not sending packets for now")

  return ret;
}
