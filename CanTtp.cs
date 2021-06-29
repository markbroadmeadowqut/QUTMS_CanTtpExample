namespace TCPGUI
{
    class CanTtp
    {
        private enum CanTtpState {
				HEADER,
                ID3,
                ID2,
				ID1,
                ID0,
				DATA,
				PAD
		}
		private static CanTtpState cts = CanTtpState.HEADER;
		private static uint id;
		private static byte[] data;
        private static bool isExtended;
        private static bool isData;
        private static int dataCount;
        private static int dataLen;

		private static void Service(byte b) {
            bool resync = false;
            do {
                switch(cts) {
                    case CanTtpState.HEADER:
                        resync = false;
                        if (((b & 0x30) == 0x00) && ((b & 0x0F) < 8)) {
                            isExtended = ((b & 0x80) == 0x80);
                            isData = ((b & 0x40) == 0x00);
                            dataLen = (b & 0x0F);
                            cts = CanTtpState.ID3;
                            id = 0;
                        }
                    break;
                    case CanTtpState.ID3:
                        if (isExtended) {
                            if (b < 32) {
                                id = b;
                                id = id << 8;
                                cts = CanTtpState.ID2;
                            } else {
                                cts = CanTtpState.HEADER;
                                resync = true;
                            }
                        } else  {
                            if (b == 0) {
                                cts = CanTtpState.ID2;
                            } else {
                                cts = CanTtpState.HEADER;
                                resync = true;
                            }
                        }
                        break;
                    case CanTtpState.ID2:
                        if (isExtended) {
                            id += b;
                            id = id << 8;
                            cts = CanTtpState.ID1;
                        } else  {
                            if (b == 0) {
                                cts = CanTtpState.ID1;
                            } else {
                                cts = CanTtpState.HEADER;
                                resync = true;
                            }
                        }
                        break;
                    case CanTtpState.ID1:
                        if (isExtended) {
                            id += b;
                            id = id << 8;
                            cts = CanTtpState.ID0;
                        } else  {
                            if (b < 8) {
                                id = b;
                                id = id << 8;
                                cts = CanTtpState.ID0;
                            } else {
                                cts = CanTtpState.HEADER;
                                resync = true;
                            }
                        }
                        break;
                    case CanTtpState.ID0:
                        id += b;
                        id = id << 8;
                        cts = CanTtpState.DATA;
                        break;
                    case CanTtpState.DATA:
                        break;
                }
            } while (resync);
		}
    }
}