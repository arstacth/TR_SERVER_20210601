using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.User;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class eServer_GET_ITEM_COLLECTION_INFO_REQ : NetPacket
	{
		public eServer_GET_ITEM_COLLECTION_INFO_REQ(string UserName, bool bOtherUser, UserItemCollectionInfo info, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GET_ITEM_COLLECTION_INFO_ACK);
			ns.Write(0);
			ns.WriteBIG5Fixed_intSize(UserName);
			ns.Write(bOtherUser);
			ns.Write(info.point);
			ns.Write(0);
			ns.Write(info.rank);
			ns.Write(last);
		}
	}
}
