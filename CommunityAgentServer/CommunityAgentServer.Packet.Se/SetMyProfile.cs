using LocalCommons.Network;

namespace CommunityAgentServer.Packet.Send
{
	public sealed class SetMyProfile : NetPacket
	{
		public SetMyProfile()
			: base(3, 0)
		{
			ns.WriteOP(15);
			ns.WriteOP(2);
			ns.Write((byte)0);
		}
	}
}
