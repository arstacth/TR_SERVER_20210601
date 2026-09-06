using System;
using System.Linq;
using System.Net;
using LocalCommons.Network;
using LocalCommons.Utilities;
using RelayServer.Structuring.Opcode;

namespace RelayServer.Network.Packet.Send
{
	public sealed class Connect_04FF5604 : NetPacket
	{
		public Connect_04FF5604(IPEndPoint remoteIpEndPoint)
			: base(2, 0)
		{
			ns.WriteOP(Opcodes.eRelayServer_REGIST_ACK);
			ns.Write((short)2);
			ushort value = (ushort)remoteIpEndPoint.Port;
			string addr = remoteIpEndPoint.Address.MapToIPv4().ToString();
			byte[] buffer = BitConverter.GetBytes(value).Reverse().ToArray();
			byte[] buffer2 = BitConverter.GetBytes(Utility.IPToInt(addr)).Reverse().ToArray();
			ns.Write(buffer, 0, 2);
			ns.Write(buffer2, 0, 4);
			ns.Fill(8);
		}
	}
}
