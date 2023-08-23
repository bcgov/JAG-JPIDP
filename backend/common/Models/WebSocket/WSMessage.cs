namespace Common.Models.WebSocket;

using System;
using System.Text;

public class WSMessage
{
    public MessageType MessageType { get; set; }
    public string Data { get; set; }
    public string TargetUser { get; set; }

    public WSMessage(MessageType messageType, string data, string targetUser)
    {
        this.MessageType = messageType;
        this.Data = data;
        this.TargetUser = targetUser;
    }

    public override string ToString() => "" + (int)this.MessageType + "|" + this.Data + "|" + this.TargetUser;

    public byte[] GetMessageBytes() => Encoding.UTF8.GetBytes(this.ToString());

    public ArraySegment<byte> GetMessageSegment()
    {
        var bytes = this.GetMessageBytes();
        return new ArraySegment<byte>(bytes, 0, bytes.Length);
    }

    public static ArraySegment<byte> GetMessage(MessageType messageType, string data, string? targetUser)
    {
        var str = "" + (int)messageType + "|" + data + "|" + (string.IsNullOrEmpty(targetUser) ? "" : targetUser);
        var bytes = Encoding.UTF8.GetBytes(str);
        return new ArraySegment<byte>(bytes, 0, bytes.Length);
    }
}

public enum MessageType
{
    Approval,
    Broadcast,
    NewUser,
    ErrorMessage
}
