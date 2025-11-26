#include "BMA250.h"
#include "SampleBuffer.h"
#include <RTCZero.h>
#include <SPI.h>
#include <STBLE.h>
#include <TinyScreen.h>
#include <Wire.h>
#include <time.h>

#define SerialMonitorInterface SerialUSB
#if BLE_DEBUG
#include <stdio.h>
extern char sprintbuff[100];
#define PRINTF(...) \
  { \
    sprintf(sprintbuff, __VA_ARGS__); \
    SerialMonitorInterface.print(sprintbuff); \
  }
#else
#define PRINTF(...)
#endif

#define BLE_DEVICE_NAME "TapPioca"

#define TAP_THRESH 5600.0
#define TAP_DEBOUNCE 60000  // microseconds
#define SHAKE_THRESH 510.0
#define SHAKE_DEBOUNCE 130000  // microseconds

#define VIBRATE_PIN 6
#define VIBRATE_PIN_ACTIVE HIGH
#define VIBRATE_PIN_INACTIVE LOW

#define FLAG_IDLE 0x00
#define FLAG_TAP 0x01
#define FLAG_SHAKE_START 0x02
#define FLAG_SHAKE_END 0x04

#define BLE_DEBUG true
#define menu_debug_print true

char sprintbuff[100];

uint32_t doVibrate = 0;

uint8_t ble_rx_buffer[21];
uint8_t ble_rx_buffer_len = 0;
uint8_t ble_can_sleep = false;
uint8_t ble_connection_state = false;
uint8_t ble_connection_displayed_state = true;

TinyScreen display = TinyScreen(TinyScreenDefault);
RTCZero RTCZ;
SampleBuffer samples;
BMA250 accel_sensor;
uint32_t clock_micros;
uint32_t last_tap = 0, last_shake = 0;
bool is_shaking = false;
uint8_t packetFlags;

uint32_t startTime = 0;
uint32_t sleepTime = 0;
unsigned long millisOffsetCount = 0;

uint8_t defaultFontColor = TS_8b_White;
uint8_t defaultFontBG = TS_8b_Black;
uint8_t inactiveFontColor = TS_8b_Gray;
uint8_t inactiveFontBG = TS_8b_Black;

uint8_t topBarHeight = 10;
uint8_t timeY = 14;
uint8_t menuTextY[4] = { 12, 25, 38, 51 };

unsigned long lastReceivedTime = 0;

unsigned long batteryUpdateInterval = 10000;
unsigned long lastBatteryUpdate = 0;

unsigned long sleepTimer = 0;
int sleepTimeout = 5;

uint8_t rewriteTime = true;

uint32_t clockDelay = -1;

uint8_t displayOn = 0;
uint8_t buttonReleased = 1;
uint8_t rewriteMenu = false;
uint8_t amtNotifications = 0;
uint8_t lastAmtNotificationsShown = -1;
unsigned long mainDisplayUpdateInterval = 300;
unsigned long lastMainDisplayUpdate = 0;
char notificationLine1[20] = "";
char notificationLine2[20] = "";

uint8_t vibratePin = 6;
uint8_t vibratePinActive = HIGH;
uint8_t vibratePinInactive = LOW;

int brightness = 3;
uint8_t lastSetBrightness = 100;

const FONT_INFO &font10pt = thinPixel7_10ptFontInfo;
const FONT_INFO &font22pt = liberationSansNarrow_22ptFontInfo;

uint32_t millisOffset() {
  return (millisOffsetCount * 1000ul) + millis();
}

void wakeHandler() {
  if (sleepTime) {
    millisOffsetCount += (RTCZ.getEpoch() - sleepTime);
    sleepTime = 0;
  }
}

void RTCwakeHandler() {
  // not used
}

void watchSleep() {
  if (doVibrate || ble_can_sleep)
    return;
  sleepTime = RTCZ.getEpoch();
  RTCZ.standbyMode();
}

void setup() {
  RTCZ.begin();
  RTCZ.setTime(16, 15, 1);  // h,m,s
  RTCZ.setDate(25, 7, 16);  // d,m,y
  Wire.begin();
  SerialMonitorInterface.begin(115200);
  display.begin();
  display.setFlip(true);
  pinMode(vibratePin, OUTPUT);
  digitalWrite(vibratePin, vibratePinInactive);
  initHomeScreen();
  requestScreenOn();
  delay(100);
  BLEsetup();

  PRINTF("Initializing BMA...");
  if (accel_sensor.begin(BMA250_range_2g, BMA250_update_time_4ms)) {
    PRINTF("ERROR! NO BMA250 DETECTED!");
  }
}


void loop() {
  Sample sample = accel_sensor.read();
  samples.append(sample);
  if (clock_micros - last_tap > TAP_DEBOUNCE) {
    packetFlags |= detectTap();
  }
  packetFlags |= detectShake();

  if (packetFlags) {
    uint8_t packet[5];
    packInt32(packet, clockDelay);
    packet[4] = packetFlags;
    // PRINTF("%d\n", microsOffset());
    PRINTF("write took: %lu\n", delay);
    lib_aci_send_data(0, packet, 5);
    packetFlags = 0;
  }

  aci_loop();  // Process any ACI commands or events from the NRF8001- main BLE
               // handler, must run often. Keep main loop short.
  if (ble_rx_buffer_len) {
    if (ble_rx_buffer[0] == 'D') {
      // expect date/time string- example: D2015 03 05 11 48 42
      lastReceivedTime = millisOffset();
      updateTime(ble_rx_buffer + 1);
      requestScreenOn();
    }
    if (ble_rx_buffer[0] == '1') {
      memcpy(notificationLine1, ble_rx_buffer + 1, ble_rx_buffer_len - 1);
      notificationLine1[ble_rx_buffer_len - 1] = '\0';
      amtNotifications = 1;
      requestScreenOn();
    }
    if (ble_rx_buffer[0] == '2') {
      memcpy(notificationLine2, ble_rx_buffer + 1, ble_rx_buffer_len - 1);
      notificationLine2[ble_rx_buffer_len - 1] = '\0';
      amtNotifications = 1;
      requestScreenOn();
      rewriteMenu = true;
      updateMainDisplay();
      doVibrate = millisOffset();
    }
    ble_rx_buffer_len = 0;
  }

  if (doVibrate) {
    uint32_t td = millisOffset() - doVibrate;
    if (td > 0 && td < 100) {
      digitalWrite(vibratePin, vibratePinActive);
    } else if (td > 200 && td < 300) {
      digitalWrite(vibratePin, vibratePinActive);
    } else {
      digitalWrite(vibratePin, vibratePinInactive);
      if (td > 300)
        doVibrate = 0;
    }
  }
  if (displayOn && (millisOffset() > mainDisplayUpdateInterval + lastMainDisplayUpdate)) {
    updateMainDisplay();
  }
  if (millisOffset() > sleepTimer + ((unsigned long)sleepTimeout * 1000ul)) {
    if (displayOn) {
      displayOn = 0;
      display.off();
    }
#if defined(ARDUINO_ARCH_SAMD)
    // watchSleep();
#endif
  }
  checkButtons();

  
  clockDelay = micros() - clock_micros;
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

void updateTime(uint8_t * b) {
  int y, M, d, k, m, s;
  char * next;
  y = strtol((char *)b, &next, 10);
  M = strtol(next, &next, 10);
  d = strtol(next, &next, 10);
  k = strtol(next, &next, 10);
  m = strtol(next, &next, 10);
  s = strtol(next, &next, 10);
  RTCZ.setTime(k, m, s);
  RTCZ.setDate(d, M, y - 2000);
}

int requestScreenOn() {
  sleepTimer = millisOffset();
  if (!displayOn) {
    displayOn = 1;
    updateMainDisplay();
    display.on();
    return 1;
  }
  return 0;
}

void checkButtons() {
  byte buttons = display.getButtons();
  if (buttonReleased && buttons) {
    if (displayOn)
      buttonPress(buttons);
    requestScreenOn();
    buttonReleased = 0;
  }
  if (!buttonReleased && !(buttons & 0x0F)) {
    buttonReleased = 1;
  }
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

uint32_t unpackInt32(uint8_t *src) {
  uint32_t val = src[3];
  val <<= 8;
  val = src[2];
  val <<= 8;
  val = src[1];
  val <<= 8;
  return val | src[0];
}

void packInt32(uint8_t *d, uint32_t val) {
  d[0] = val;
  d[1] = val >> 8;
  d[2] = val >> 16;
  d[3] = val >> 24;
}
