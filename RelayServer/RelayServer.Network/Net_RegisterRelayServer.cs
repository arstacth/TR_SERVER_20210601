using LocalCommons.Network;

namespace RelayServer.Network
{
	public sealed class Net_RegisterRelayServer : NetPacket
	{
		public Net_RegisterRelayServer()
			: base(0)
		{
			ns.Write((byte)0);
			ns.Write((byte)1);
			ns.Write((short)9155);
			ns.WriteDynamicASCII(Conf.ServerIP);
			ns.WriteDynamicASCII("");
		}
	}
}
