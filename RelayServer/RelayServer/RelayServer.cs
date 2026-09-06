using System.Net;
using Akka.Actor;
using Akka.IO;
using Serilog;

namespace RelayServer
{
	public class RelayServer : ReceiveActor
	{
		public static IActorRef server;

		private IPEndPoint _address;

		public RelayServer(IActorRef handler, IPEndPoint endpoint)
		{
			_address = endpoint;
			Receive<Udp.Bound>(delegate
			{
				server = base.Sender;
				Log.Information("RelayServer is listening on {0}:{1}", Conf.ServerIP, Conf.RelayPort);
			});
			UntypedActor.Context.System.Udp().Tell(new Udp.Bind(handler, endpoint));
		}
	}
}
