using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using LocalCommons.Network;

namespace LoadBalanceServer.Packet.Send
{
	public sealed class HashCheckOK : NetPacket
	{
		public HashCheckOK()
			: base(3, 0)
		{
			ns.WriteOP(2);
			ns.WriteBIG5Fixed_shortSize(Conf.ServerIP);
			if (Conf.AgentPortList.Count > 0)
			{
				if (Conf.AgentPortList.TryGetValue(Conf.AgentClientBalance.OrderBy((KeyValuePair<IActorRef, int> o) => o.Value).FirstOrDefault().Key, out var value))
				{
					ns.Write(value);
				}
				else
				{
					ns.Write(Conf.AgentPort);
				}
			}
			else
			{
				ns.Write(Conf.AgentPort);
			}
			ns.Write((byte)1);
		}
	}
}
