namespace BinBridge.Ssh
{
    internal interface IClientAuthentication
    {
        void Authenticate(IConnectionInfoInternal connectionInfo, ISession session);
    }
}
