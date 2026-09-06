using LocalCommons.Network;

namespace LoadBalanceServer.Packet.Send
{
	public sealed class HashCheckFail : NetPacket
	{
		public HashCheckFail()
			: base(3, 0)
		{
			ns.WriteOP(2);
			ns.Write((short)0);
			ns.Write(0);
			ns.Write((byte)0);
		}
	}
}
