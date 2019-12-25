using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace SiS.Communication.Business
{
    /// <summary>
    /// Represents a general business message 
    /// | Header(4bytes) |  MessageType(string) | Body(string) | Payload(bytes) |
    /// </summary>
    public class GeneralMessage
    {
        #region Properties

        /// <summary>
        /// Gets or sets a 32bit-integer indicating how to use the message.
        /// </summary>
        public int Header { get; set; }

        /// <summary>
        /// Gets or sets a string indicating how to use the Body.
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Gets or sets string value indicating the message body.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets byte values indicating the message payload.
        /// </summary>
        public ArraySegment<byte> Payload { get; set; }

        /// <summary>
        /// Gets a value that indicates whether the Playload using a outside buffer or inside buffer.
        /// </summary>
        /// <returns>true if inner buffer is used; otherwise, false. The default is false.</returns>
        public bool IsDetached
        {
            get { return _innerBuffer != null; }
        }

        #endregion

        #region Private Members
        private byte[] _innerBuffer;
        #endregion

        #region Public Functions

        /// <summary>
        /// Detach outside buffer and copy the outside data to inner buffer.
        /// </summary>
        public void Detach()
        {
            if (IsDetached)
            {
                return;
            }
            if (Payload != null)
            {
                _innerBuffer = new byte[Payload.Count];
                Buffer.BlockCopy(Payload.Array, Payload.Offset, _innerBuffer, 0, _innerBuffer.Length);
                Payload = new ArraySegment<byte>(_innerBuffer);
            }
        }

        /// <summary>
        /// Serialize to byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            Contract.Requires(!string.IsNullOrEmpty(MessageType) && !string.IsNullOrEmpty(Body));
            MemoryStream ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8);
            writer.Write(Header);
            writer.Write(MessageType);
            writer.Write(Body);
            if (Payload != null && Payload.Count > 0)
            {
                writer.Write(Payload.Count);
                writer.Write(Payload.Array, Payload.Offset, Payload.Count);
            }
            byte[] data = ms.ToArray();
            writer.Close();
            return data;
        }

        /// <summary>
        /// Deserialize byte array segment into GeneralMessage.
        /// </summary>
        /// <param name="messageData">The message data to deserialize.</param>
        /// <param name="isDetach">True if detach the outside data; false to use outside buffer.</param>
        /// <returns>The instance of GeneralMessage class.</returns>
        public static GeneralMessage Deserialize(ArraySegment<byte> messageData, bool isDetach = true)
        {
            return Deserialize(messageData.Array, messageData.Offset, messageData.Count, isDetach);
        }

        /// <summary>
        /// Deserialize byte array into GeneralMessage.
        /// </summary>
        /// <param name="messageData">The message data to deserialize.</param>
        /// <param name="isDetach">True if detach the outside data; false to use outside buffer.</param>
        /// <returns>The instance of GeneralMessage class.</returns>
        public static GeneralMessage Deserialize(byte[] messageData, bool isDetach = true)
        {
            return Deserialize(messageData, 0, messageData.Length, isDetach);
        }

        /// <summary>
        /// Deserialize byte array into GeneralMessage.
        /// </summary>
        /// <param name="messageData">The message data to deserialize.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of the message data to deserialize.</param>
        /// <param name="isDetach">True if detach the outside data; false to use outside buffer.</param>
        /// <returns>The instance of GeneralMessage class.</returns>
        public static GeneralMessage Deserialize(byte[] messageData, int offset, int count, bool isDetach = true)
        {
            BinaryReader reader = null;
            try
            {
                MemoryStream ms = new MemoryStream(messageData, offset, count);
                reader = new BinaryReader(ms, Encoding.UTF8);
                GeneralMessage msg = new GeneralMessage();
                msg.Header = reader.ReadInt32();
                msg.MessageType = reader.ReadString();
                msg.Body = reader.ReadString();
                if (ms.Position < count)
                {
                    int payloadLen = reader.ReadInt32();
                    int remainDataLen = messageData.Length - (int)ms.Position - offset;
                    if (remainDataLen < payloadLen)
                    {
                        return null;
                    }
                    if (isDetach)
                    {
                        msg._innerBuffer = reader.ReadBytes(payloadLen);
                        msg.Payload = new ArraySegment<byte>(msg._innerBuffer);
                    }
                    else
                    {
                        msg.Payload = new ArraySegment<byte>(messageData, offset + (int)ms.Position, payloadLen);
                    }
                }
                return msg;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
        }

        #endregion
    }
}
