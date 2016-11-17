﻿using System;
using BinBridge.Ssh.Sftp.Responses;

namespace BinBridge.Ssh.Sftp.Requests
{
    internal class SftpFStatRequest : SftpRequest
    {
        public override SftpMessageTypes SftpMessageType
        {
            get { return SftpMessageTypes.FStat; }
        }

        public byte[] Handle { get; private set; }

        /// <summary>
        /// Gets the size of the message in bytes.
        /// </summary>
        /// <value>
        /// The size of the messages in bytes.
        /// </value>
        protected override int BufferCapacity
        {
            get
            {
                var capacity = base.BufferCapacity;
                capacity += 4; // Handle length
                capacity += Handle.Length; // Handle
                return capacity;
            }
        }

        public SftpFStatRequest(uint protocolVersion, uint requestId, byte[] handle, Action<SftpAttrsResponse> attrsAction, Action<SftpStatusResponse> statusAction)
            : base(protocolVersion, requestId, statusAction)
        {
            Handle = handle;
            SetAction(attrsAction);
        }

        protected override void LoadData()
        {
            base.LoadData();
            Handle = ReadBinary();
        }

        protected override void SaveData()
        {
            base.SaveData();
            WriteBinaryString(Handle);
        }
    }
}
