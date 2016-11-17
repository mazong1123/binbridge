using BinBridge.Ssh.Common;

namespace BinBridge.Ssh.Sftp.Responses
{
    internal class SftpExtendedReplyResponse : SftpResponse
    {
        public override SftpMessageTypes SftpMessageType
        {
            get { return SftpMessageTypes.ExtendedReply; }
        }

        public SftpExtendedReplyResponse(uint protocolVersion)
            : base(protocolVersion)
        {
        }

        public T GetReply<T>() where T : SshData, new()
        {
            return OfType<T>();
        }
    }
}
