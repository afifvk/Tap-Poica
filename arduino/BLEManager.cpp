#include "BLEManager.h"
#include "globals.h"
#include <STBLE.h>

#define ADV_INTERVAL_MIN_MS 100
#define ADV_INTERVAL_MAX_MS 200
#define CONN_INTERVAL_MIN_MS 50
#define CONN_INTERVAL_MAX_MS 100

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

uint8_t hexToNib(char h) {
  if (h >= '0' && h <= '9')
    return h - '0';
  else if (h >= 'a' && h <= 'f')
    return h - 'a' + 10;
  else if (h >= 'A' && h <= 'F')
    return h - 'A' + 10;
  return 0;
}

void uuidStrToByte(const char *in, uint8_t *out, uint8_t byte_size) {
  uint8_t nibCount = 0;
  uint8_t temp = 0;
  if (*in == '0' && *(in + 1) == 'x')
    in += 2;
  while (*in && nibCount < (byte_size / 4)) {
    if (isalnum(*in)) {
      if ((nibCount & 1) == 0) {
        temp = (hexToNib(*in) << 4) & 0xF0;
      } else {
        temp |= hexToNib(*in);
        out[(byte_size / 8) - 1 - (nibCount / 2)] = temp;
      }
      nibCount++;
    }
    in++;
  }
  PRINTF("UUID: ");
  for (int i = 0; i < (byte_size / 8); i++) {
    PRINTF("%02X", out[i]);
  }
  PRINTF("\n");
}

BLEManager *BLEManager::instance = NULL;

BLEManager::BLEManager(const char *deviceName, const char *advUUID,
                       void (*onConnect)(void), void (*onDisconnect)(void),
                       void (*onBond)(void))
    : _bleRxBufferLen(0), bleConnectionState(false), _setConnectable(1),
      _connectionHandle(0), _connected(false), _lastProcedureCompleted(0),
      _deviceName(deviceName), _advUUID(advUUID) {
  phoneConnection.onConnect = onConnect;
  phoneConnection.onDisconnect = onDisconnect;
  phoneConnection.onBond = onBond;
}

void BLEManager::setInstance(BLEManager *inst) { instance = inst; }

void BLEManager::begin() {
  setInstance(this);

  HCI_Init();
  /* Init SPI interface */
  BNRG_SPI_Init();
  /* Reset BlueNRG/BlueNRG-MS SPI interface */
  BlueNRG_RST();

  uint8_t bdaddr[] = {0x12, 0x34, 0x00, 0xE1, 0x80, 0x02};

  // Setting bdaddr
  uint8_t ret = aci_hal_write_config_data(CONFIG_DATA_PUBADDR_OFFSET,
                                          CONFIG_DATA_PUBADDR_LEN, bdaddr);

  if (ret) {
    PRINTF("Setting BD_ADDR failed.\n");
  }

  // Init GATT
  ret = aci_gatt_init();

  if (ret) {
    PRINTF("GATT_Init failed.\n");
  }

  uint8_t deviceNameLen = strlen(_deviceName);
  uint16_t serviceHandle, _deviceNameCharHandle, appearanceCharHandle;
  ret = aci_gap_init_IDB05A1(GAP_PERIPHERAL_ROLE_IDB05A1, 0, deviceNameLen,
                             &serviceHandle, &_deviceNameCharHandle,
                             &appearanceCharHandle);

  if (ret) {
    PRINTF("GAP_Init failed.\n");
  }

  ret = aci_gatt_update_char_value(serviceHandle, _deviceNameCharHandle, 0,
                                   deviceNameLen, (uint8_t *)_deviceName);

  if (ret) {
    PRINTF("aci_gatt_update_char_value failed.\n");
  } else {
    PRINTF("BLE Stack setupServicesd.\n");
  }

  setupServices();

  if (ret) {
    PRINTF("Error while adding UART characteristic.\n");
  }

  /* +4 dBm output power */
  ret = aci_hal_set_tx_power_level(1, 3);

  ret = aci_gap_set_io_capability(IO_CAP_NO_INPUT_NO_OUTPUT);
  if (ret) {
    PRINTF("aci_gap_set_io_capability failed.\n");
  }

  ret = aci_gap_set_auth_requirement(
      MITM_PROTECTION_NOT_REQUIRED, OOB_AUTH_DATA_ABSENT, NULL,
      MIN_ENCRY_KEY_SIZE,              /* Min. encryption key size */
      MAX_ENCRY_KEY_SIZE,              /* Max encryption key size */
      DONOT_USE_FIXED_PIN_FOR_PAIRING, /* no fixed pin */
      0,                               /* fixed pin not used */
      BONDING);
  if (ret) {
    PRINTF("aci_gap_set_auth_requirement failed.\n");
  }
}

void BLEManager::update() {
  HCI_Process();
  bleConnectionState = _connected;
  if (_setConnectable) {
    advertise();
    _setConnectable = 0;
  }
  if (HCI_Queue_Empty()) {
    // Enter_LP_Sleep_Mode();
  }
}

void BLEManager::advertise() {
  // uint8_t UUID[18];
  // if (strlen(_advUUID) < 8) {
  //   UUID[0] = 3;
  //   UUID[1] = AD_TYPE_SERV_SOLICIT_16_BIT_UUID_LIST;
  //   uuidStrToByte(_advUUID, UUID + 2, 16);
  // } else {
  //   UUID[0] = 17;
  //   UUID[1] = AD_TYPE_SERV_SOLICIT_128_BIT_UUID_LIST;
  //   uuidStrToByte(_advUUID, UUID + 2, 128);
  // }

  // tBleStatus ret = hci_le_set_scan_resp_data(UUID[0] + 1, UUID);
  // if (ret != BLE_STATUS_SUCCESS)
  //   PRINTF("Set scan resp error: %d\n", (uint8_t)ret);

  tBleStatus ret = hci_le_set_scan_resp_data(0, NULL);

  char localName[32] = {AD_TYPE_COMPLETE_LOCAL_NAME};
  strcpy(localName + 1, _deviceName);

  ret = aci_gap_set_discoverable(
      ADV_IND, ((uint32_t)ADV_INTERVAL_MIN_MS * 1000ul) / 625ul,
      ((uint32_t)ADV_INTERVAL_MAX_MS * 1000ul) / 625ul, STATIC_RANDOM_ADDR,
      NO_WHITE_LIST_USE, strlen(_deviceName) + 1, localName, 0, NULL,
      ((uint32_t)CONN_INTERVAL_MIN_MS * 1000ul) / 625ul,
      ((uint32_t)CONN_INTERVAL_MAX_MS * 1000ul) / 625ul);

  if (ret != BLE_STATUS_SUCCESS) {
    PRINTF("Advertise failed with error %d\n", (uint8_t)ret);
  } else {
    PRINTF("General Discoverable Mode.\n");
  }

  phoneConnection.isAdvertising = true;
}

// BLEServ *Att_Read_CB_service = NULL;
uint8_t BLEManager::addService(BLEServ *service, char *servUUID,
                               uint8_t servType, uint8_t maxAttrCount) {
  PRINTF("Adding service..\n");

  if (strlen(servUUID) < 8) {
    uuidStrToByte(servUUID, service->UUID, 16);
    service->UUIDType = UUID_TYPE_16;
  } else {
    uuidStrToByte(servUUID, service->UUID, 128);
    service->UUIDType = UUID_TYPE_128;
  }

  tBleStatus ret = aci_gatt_add_serv(service->UUIDType, service->UUID, servType,
                                     maxAttrCount, &service->handle);

  if (ret) {
    PRINTF("Error while adding service: %d\n", ret);
  }

  if (service->handle) {
    PRINTF("Added service\n");
    return BLE_STATUS_SUCCESS;
  }
  return -1;
}

// BLEChar *Att_Read_CB_characteristic = NULL;
uint8_t BLEManager::addCharacteristic(BLEServ *service, BLEChar *characteristic,
                                      char *charUUID, uint8_t charValLen,
                                      uint8_t charProps, uint8_t secPermission,
                                      uint8_t gattEventMask) {
  PRINTF("Adding characteristic..\n");
  characteristic->serviceHandle = service->handle;
  if (strlen(charUUID) < 8) {
    uuidStrToByte(charUUID, characteristic->UUID, 16);
    characteristic->UUIDType = UUID_TYPE_16;
  } else {
    uuidStrToByte(charUUID, characteristic->UUID, 128);
    characteristic->UUIDType = UUID_TYPE_128;
  }

  tBleStatus ret = aci_gatt_add_char(
      characteristic->serviceHandle, characteristic->UUIDType,
      characteristic->UUID, charValLen, charProps, secPermission, gattEventMask,
      MAX_ENCRY_KEY_SIZE, 1, &characteristic->handle);
  if (ret) {
    PRINTF("Error while adding characteristic to service: %d\n", ret);
  }
  if (!characteristic->handle)
    return -1;

  PRINTF("Added characteristic\n");

  return BLE_STATUS_SUCCESS;
}

// uint8_t BLEManager::write(char *payload, uint8_t dataSize) {
//   tBleStatus ret = aci_gatt_update_char_value(
//       primaryService.handle, uartRxChar.handle, 0, dataSize, (uint8_t
//       *)payload);

//   if (ret == BLE_STATUS_SUCCESS)
//     return ret;

//   PRINTF("Error while updating UART characteristic.\n");
//   return BLE_STATUS_ERROR;
// }

uint8_t *BLEManager::getRxBuffer() { return _bleRxBuffer; }

uint8_t BLEManager::getRxBufferLen() { return _bleRxBufferLen; }

void BLEManager::resetRxBuffer() { _bleRxBufferLen = 0; }

void BLEManager::onReadRequest(uint16_t handle) {
  if (!instance)
    return;
  /*if(handle == UARTTxChar->Handle + 1) {

    } else if(handle == UARTRxChar->Handle + 1){
    }*/

  PRINTF("Read Request\n");
  if (instance->_connectionHandle != 0)
    aci_gatt_allow_read(instance->_connectionHandle);
}

void BLEManager::onAttributeRead(uint16_t handle, uint8_t dataLength,
                                 uint8_t attrDataLength, uint8_t *attrData) {
  PRINTF("Attribute Read\n");
  PRINTF("%04X, %d, %d\n", handle, dataLength, attrDataLength);
}

void BLEManager::onAttributeModified(uint16_t handle, uint8_t dataLength,
                                     uint8_t *data) {
  if (!instance)
    return;
  PRINTF("Attribute Modified\n");
  // if (handle != instance->uartTxChar.handle + 1)
  //   return;
  int i;
  for (i = 0; i < dataLength; i++) {
    instance->_bleRxBuffer[i] = data[i];
  }
  instance->_bleRxBuffer[i] = '\0';
  instance->_bleRxBufferLen = dataLength;
  if (data[0] != 0) {
    PRINTF("Updated value to %u %lu\n", data[0],
           unpackInt32((uint8_t *)&data[1]));
  }
}

void BLEManager::onAttributeNotification(uint16_t handle, uint8_t dataLength,
                                         uint16_t attrHandle,
                                         uint8_t *attrValue) {
  if (!instance)
    return;
  PRINTF("Attribute_Notification_CB %04X, %04X, %d\n", handle, attrHandle,
         dataLength);
  for (int i = 0; i < dataLength - 2; i++) {
    PRINTF("%02X, ", attrValue[i]);
  }
  PRINTF("\n");
  for (int i = 0; i < dataLength - 2; i++) {
    PRINTF("%c", attrValue[i]);
  }
  PRINTF("\n");

  // BLEChar *testChar = (BLEChar *)instance->phoneConnection.firstChar;
  // while (testChar) {
  //   if (attrHandle == testChar->valueHandle && testChar->onUpdate) {
  //     testChar->onUpdate(attrValue, dataLength - 2);
  //     return;
  //   }
  //   testChar = (BLEChar *)testChar->nextCharacteristic;
  // }
  // PRINTF("Unhandled update!\n");

  // atrributeUpdateHandler(attr_handle, attr_value, data_length - 2);
}

void BLEManager::onPairComplete(uint8_t status) {
  if (!instance)
    return;

  if (status)
    return;

  PRINTF("Pairing complete: %02X\n", status);

  instance->phoneConnection.isBonded = true;
  if (instance->phoneConnection.onBond)
    instance->phoneConnection.onBond();
}

void BLEManager::onUnpairComplete(void) {
  if (!instance)
    return;
  PRINTF("bond lost, allowing rebond\n")

  aci_gap_allow_rebond_IDB05A1(instance->phoneConnection.handle);
}

void BLEManager::onGAPConnectionComplete(uint8_t addr[6], uint16_t handle) {
  if (!instance)
    return;
  instance->_connected = true;
  instance->_connectionHandle = handle;

  PRINTF("Connected to device:");
  for (int i = 5; i > 0; i--) {
    PRINTF("%02X-", addr[i]);
  }
  PRINTF("%02X\r\n", addr[0]);
}

void BLEManager::onGAPDisconnectionComplete(void) {
  if (!instance)
    return;
  instance->_connected = false;
  PRINTF("Disconnected\n");
  /* Make the device connectable again. */
  instance->_setConnectable = TRUE;
}

void HCI_Event_CB(void *pckt) {
  hci_uart_pckt *hci_pckt = (hci_uart_pckt *)pckt;
  hci_event_pckt *event_pckt = (hci_event_pckt *)hci_pckt->data;

  if (hci_pckt->type != HCI_EVENT_PKT)
    return;

  PRINTF("%lu PCKT START: ", millis());

  switch (event_pckt->evt) {

  case EVT_DISCONN_COMPLETE: {
    // evt_disconn_complete *evt = (void *)event_pckt->data;
    BLEManager::onGAPDisconnectionComplete();
  } break;

  case EVT_LE_META_EVENT: {
    evt_le_meta_event *evt = (evt_le_meta_event *)event_pckt->data;

    switch (evt->subevent) {
    case EVT_LE_CONN_COMPLETE: {
      evt_le_connection_complete *cc = (evt_le_connection_complete *)evt->data;
      BLEManager::onGAPConnectionComplete(cc->peer_bdaddr, cc->handle);
    } break;
    default: {
      PRINTF("EVT_LE_META_EVENT: %04X\n", evt->subevent);
    }
    }
  } break;

  case EVT_VENDOR: {
    evt_blue_aci *blue_evt = (evt_blue_aci *)event_pckt->data;
    switch (blue_evt->ecode) {

    case EVT_BLUE_ATT_READ_BY_GROUP_TYPE_RESP: {
      evt_att_read_by_group_resp *gr =
          (evt_att_read_by_group_resp *)blue_evt->data;
      BLEManager::onAttributeRead(gr->conn_handle, gr->event_data_length,
                                  gr->attribute_data_length,
                                  gr->attribute_data_list);
    } break;

    case EVT_BLUE_ATT_FIND_BY_TYPE_VAL_RESP: {
      evt_att_find_by_type_val_resp *tv =
          (evt_att_find_by_type_val_resp *)blue_evt->data;
      BLEManager::onAttributeRead(tv->conn_handle, tv->event_data_length,
                                  tv->event_data_length, tv->handles_info_list);
    } break;

    case EVT_BLUE_GATT_READ_PERMIT_REQ: {
      evt_gatt_read_permit_req *pr = (evt_gatt_read_permit_req *)blue_evt->data;
      BLEManager::onReadRequest(pr->attr_handle);
    } break;

    case EVT_BLUE_GATT_ATTRIBUTE_MODIFIED: {
      evt_gatt_attr_modified_IDB05A1 *evt =
          (evt_gatt_attr_modified_IDB05A1 *)blue_evt->data;
      BLEManager::onAttributeModified(evt->attr_handle, evt->data_length,
                                      evt->att_data);
    } break;

    case EVT_BLUE_GATT_NOTIFICATION: {
      evt_gatt_attr_notification *n =
          (evt_gatt_attr_notification *)blue_evt->data;
      BLEManager::onAttributeNotification(n->conn_handle, n->event_data_length,
                                          n->attr_handle, n->attr_value);
    } break;

    case EVT_BLUE_GAP_PAIRING_CMPLT: {
      evt_gap_pairing_cmplt *pc = (evt_gap_pairing_cmplt *)blue_evt->data;
      BLEManager::onPairComplete(pc->status);
    } break;

    case EVT_BLUE_GATT_ERROR_RESP: {
      evt_gatt_error_resp *er = (evt_gatt_error_resp *)blue_evt->data;
      PRINTF("err: %02X, %04X, %02X\n", er->req_opcode, er->attr_handle,
             er->error_code);
    } break;

    case EVT_BLUE_GAP_BOND_LOST: {
      BLEManager::onUnpairComplete();
    } break;

    default:
      PRINTF("EVT_BLUE: %04X\n", blue_evt->ecode);
    }
  } break;
  }
}
