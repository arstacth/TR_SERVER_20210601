using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.Park;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetMachineInfoResetting : NetPacket
	{
		public GetMachineInfoResetting(Account User, int MachineNum, CapsuleMachineData MachineInfo, byte last)
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
			ns.Write((byte)0);
			ns.Write(9);
			ns.Write(0);
			ns.Fill(20);
			ns.Write(last);
		}
	}
}
