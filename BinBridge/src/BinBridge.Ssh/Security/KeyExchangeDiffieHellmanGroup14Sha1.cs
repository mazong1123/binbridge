﻿using BinBridge.Ssh.Common;
using System.Globalization;

namespace BinBridge.Ssh.Security
{
    /// <summary>
    /// Represents "diffie-hellman-group14-sha1" algorithm implementation.
    /// </summary>
    internal class KeyExchangeDiffieHellmanGroup14Sha1 : KeyExchangeDiffieHellmanGroupSha1
    {
        private const string SecondOkleyGroup = "00FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF6955817183995497CEA956AE515D2261898FA051015728E5A8AACAA68FFFFFFFFFFFFFFFF";

        /// <summary>
        /// Gets algorithm name.
        /// </summary>
        public override string Name
        {
            get { return "diffie-hellman-group14-sha1"; }
        }

        /// <summary>
        /// Gets the group prime.
        /// </summary>
        /// <value>
        /// The group prime.
        /// </value>
        public override BigInteger GroupPrime
        {
            get
            {
                BigInteger prime;
                BigInteger.TryParse(SecondOkleyGroup, NumberStyles.AllowHexSpecifier, NumberFormatInfo.CurrentInfo, out prime);
                return prime;
            }
        }
    }
}