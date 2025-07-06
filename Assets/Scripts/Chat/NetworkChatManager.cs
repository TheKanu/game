using UnityEngine;
using Mirror;  // WICHTIG: Mirror verwenden, nicht UnityEngine.Networking
/*
public class NetworkChatManager : NetworkBehaviour
{
    [Command]
    void CmdSendMessage(string message, int channelInt)
    {
        ChatChannel channel = (ChatChannel)channelInt;
        RpcReceiveMessage(connectionToClient.identity.name, message, channelInt);
    }

    [ClientRpc]
    void RpcReceiveMessage(string sender, string message, int channelInt)
    {
        ChatChannel channel = (ChatChannel)channelInt;
        FindObjectOfType<ChatManager>().ReceiveMessage(sender, message, channel);
    }
}
*/