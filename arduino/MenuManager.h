#ifndef MENUMANAGER_H
#define MENUMANAGER_H

#include "Timer.h"
#include <RTCZero.h>
#include <cstdint>

#define FONT_10_PT (FONT_INFO &)thinPixel7_10ptFontInfo
#define FONT_22_PT (FONT_INFO &)liberationSansNarrow_22ptFontInfo

#define UP_BUTTON TSButtonUpperRight
#define DOWN_BUTTON TSButtonLowerRight
#define SELECT_BUTTON TSButtonLowerLeft
#define BACK_BUTTON TSButtonUpperLeft
#define MENU_BUTTON TSButtonLowerLeft
#define VIEW_BUTTON TSButtonLowerRight
#define CLEAR_BUTTON TSButtonLowerRight

#define DEFAULT_FONT_COLOR TS_8b_White
#define DEFAULT_FONT_BG TS_8b_Black
#define INACTIVE_FONT_COLOR TS_8b_Gray
#define INACTIVE_FONT_BG TS_8b_Black

class MenuManager {
public:
  MenuManager(Timer &state, TinyScreen &ts);
  RTCZero RTCZ;
  uint8_t menuTextY[4];

  void dateTimeMenu(uint8_t selection);
  void mainMenu(uint8_t selection);
  void viewMenu(uint8_t button);
  uint8_t editInt(uint8_t button, int *inVal, char *intName,
                  void (MenuManager::*cb)());

private:
  TinyScreen &_display;
  Timer &_timer;

  uint8_t _menuHistory[5];
  uint8_t _menuHistoryIndex;
  uint8_t _currentMenu;
  uint8_t _currentMenuLine;
  uint8_t _currentSelectionLine;
  uint8_t _lastMenuLine;
  uint8_t _lastSelectionLine;

  int _currentVal;
  int _digits[4];
  int _currentDigit;
  int _maxDigit;
  int *_originalVal;
  void (MenuManager::*_editIntCallBack)();

  uint8_t _dateTimeSelection;
  int _dateTimeVariable;

  void _newMenu(int8_t newIndex);
  void _saveChangeCallback();
  void _handleButton(uint8_t button);
};

#endif
