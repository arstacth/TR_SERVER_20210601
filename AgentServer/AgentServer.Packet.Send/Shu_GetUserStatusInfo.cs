using System.Collections.Concurrent;
using System.Collections.Generic;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Shu_GetUserStatusInfo : NetPacket
	{
		public Shu_GetUserStatusInfo(ConcurrentDictionary<long, List<int>> status, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHU_PROTOCOL);
			ns.Write(8);
			ns.Write(0);
			ns.Write(status.Count);
			foreach (KeyValuePair<long, List<int>> item in status)
			{
				ns.Write(item.Key);
				ns.Write((short)16);
				foreach (int item2 in item.Value)
				{
					ns.Write(item2);
				}
			}
			ns.Write(last);
		}
	}
}
