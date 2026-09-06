using System;

namespace RelayServer.Structuring.Opcode
{
	[Flags]
	public enum Opcodes
	{
		eRelayServer_REGIST_REQ = 0x17,
		eRelayServer_REGIST_ACK = 0x18,
		eRelayServer_P2P_WRAP_REQ = 0x19,
		eRelayServer_P2P_WRAP_ACK = 0x1A,
		eRelayServer_LIVE_MSG_REQ = 0x1B,
		eRelayServer_LIVE_MSG_ACK = 0x1C
	}
}
