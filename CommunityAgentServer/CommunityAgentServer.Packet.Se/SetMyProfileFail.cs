using LocalCommons.Network;

namespace CommunityAgentServer.Packet.Send
{
	public sealed class SetMyProfileFail : NetPacket
	{
		public SetMyProfileFail()
			: base(3, 0)
		{
			ns.WriteOP(15);
			ns.WriteOP(2);
			ns.Write((byte)1);
		}
	}
}
