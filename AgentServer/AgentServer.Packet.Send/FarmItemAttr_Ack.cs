using System.Collections.Concurrent;
using System.Collections.Generic;
using AgentServer.Structuring.Farm;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class FarmItemAttr_Ack : NetPacket
	{
		public FarmItemAttr_Ack(int FarmUniqueNum, ConcurrentDictionary<long, List<FarmItemAttr>> farmitemattr, byte last)
		{
			ns.WriteOP(Opcodes.eServer_FARM_ACK);
			ns.WriteOP(FarmProtocol.FarmItemAttr_ACK);
			ns.Write(0);
			ns.Write(FarmUniqueNum);
			ns.Write((short)1);
			ns.Write(farmitemattr.Count);
			foreach (KeyValuePair<long, List<FarmItemAttr>> item in farmitemattr)
			{
				ns.Write(item.Key);
				ns.Write(item.Value.Count);
				foreach (FarmItemAttr item2 in item.Value)
				{
					ns.Write(item2.AttrType);
					ns.Write(item2.AttrValueNumber);
					ns.WriteBIG5Fixed_intSize(item2.AttrValueString);
				}
			}
			ns.Write(last);
		}
	}
}
