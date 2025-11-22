#include "Timer.h"

uint32_t millisOffset() {
  static uint32_t millisOffsetCount = 0;
  return (millisOffsetCount * 1000ul) + millis();
}

Timer::Timer(TinyScreen &ts)
    : currentDisplayState(STATE_HOME), brightness(3), sleepTimeout(5),
      displayOn(false), _sleepTimer(0), amtNotifications(0), _display(ts) {}

void Timer::requestScreenOn() {
  PRINTF("Request screen on\n");
  _sleepTimer = millisOffset();
  if (displayOn)
    return;
  displayOn = true;
  _display.on();
}

uint32_t Timer::getSleepTimer() const { return _sleepTimer; }

void Timer::handleSleep() {
  if ((millisOffset() >
       getSleepTimer() + ((uint32_t)sleepTimeout * 1000ul)) &&
      displayOn) {
    displayOn = false;
    _display.off();
  }
}
