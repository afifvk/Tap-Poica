#ifndef TIMER_H
#define TIMER_H

#include "globals.h"
#include <cstdint>

uint32_t millisOffset();

class Timer {
public:
  enum DisplayState { STATE_HOME, STATE_MENU, STATE_EDITOR };

  Timer(TinyScreen &ts);

  DisplayState currentDisplayState;
  int brightness;
  int sleepTimeout;
  bool displayOn;
  uint8_t amtNotifications;

  void requestScreenOn();
  uint32_t getSleepTimer() const;
  void handleSleep();

private:
  TinyScreen &_display;
  uint32_t _sleepTimer;
};

#endif
