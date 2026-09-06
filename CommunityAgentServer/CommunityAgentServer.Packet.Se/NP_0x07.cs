using LocalCommons.Network;

namespace CommunityAgentServer.Packet.Send
{
	public sealed class NP_0x07 : NetPacket
	{
		public NP_0x07(string nickname, byte[] remain)
			: base(3, 0)
		{
			ns.WriteOP(7);
			ns.WriteBIG5Fixed_shortSize(nickname);
			ns.Write(remain, 0);
		}
	}
}
