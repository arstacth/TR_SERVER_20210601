using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.Park;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetMachineInfo : NetPacket
	{
		public GetMachineInfo(Account User, int MachineNum, CapsuleMachineData MachineInfo, byte last)
		{
			ns.WriteOP(Opcodes.eServer_CAPSULE_MACHINE_INFO__VER2_ACK);
			ns.Write(MachineInfo.RealMachineNum);
			ns.Write(MachineInfo.RealMachineNumKind);
			if (MachineInfo.isRotate)
			{
				ns.Write(MachineNum);
			}
			else
			{
				ns.Write(-1);
			}
			ns.Write((byte)1);
			ns.Write(0);
			ns.Write(MachineInfo.ItemList.Count);
			foreach (CapsuleMachineItemNew item in MachineInfo.ItemList)
			{
				ns.Write(item.ItemNum);
				ns.Write((int)item.ItemCount);
				ns.Write((int)item.ItemMax);
				ns.Write(item.ItemNum);
				ns.Write(item.Level);
			}
			ns.Fill(20);
			ns.Write(last);
		}
	}
}
