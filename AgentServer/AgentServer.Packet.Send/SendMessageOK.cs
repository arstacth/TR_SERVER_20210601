using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class SendMessageOK : NetPacket
	{
		public SendMessageOK(string sendNickname, string recvNickName, long messageNum, long sendDateTime, string Msg, byte last)
		{
			ns.WriteOP(Opcodes.eServer_MESSAGE_SEND_ACK);
			ns.Write(0);
			ns.WriteBIG5Fixed_intSize(recvNickName);
			ns.Write(messageNum);
			ns.Write((short)0);
			ns.WriteBIG5Fixed_intSize(sendNickname);
			ns.Write(sendDateTime);
			ns.WriteBIG5Fixed_intSize(Msg);
			ns.Write(last);
		}
	}
}
