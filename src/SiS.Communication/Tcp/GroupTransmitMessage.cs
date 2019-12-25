using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiS.Communication.Tcp
{
    /// <summary>
    /// The tcp message for transmiting to specifice groups.
    /// </summary>
    internal class GroupTransmitMessage
    {
        public UInt32 GroupTransmitMessageMark { get; set; }
        public ArraySegment<byte> TransMessageData { get; set; }
        public IEnumerable<string> GroupNameCollection { get; set; }

        public static byte[] MakeGroupMessage(IEnumerable<string> groupNameCollection, ArraySegment<byte> realMessageData, bool loopBack)
        {
            //| mark(4bytes) |  group length(4bytes) | group names | real content data |
            string groupNameDes = "";
            foreach (string groupName in groupNameCollection)
            {
                groupNameDes += groupName.Replace("|", "") + "|";
            }
            groupNameDes = groupNameDes.TrimEnd('|');
            byte[] markHeaderData = BitConverter.GetBytes(loopBack ? TcpUtility.GROUP_TRANSMIT_MSG_LOOP_BACK_MARK : TcpUtility.GROUP_TRANSMIT_MSG_MARK);
            byte[] groupDesData = Encoding.UTF8.GetBytes(groupNameDes);
            byte[] groupLengthData = BitConverter.GetBytes(groupDesData.Length);

            byte[] messageData = new byte[4 + 4 + groupDesData.Length + realMessageData.Count];

            Buffer.BlockCopy(markHeaderData, 0, messageData, 0, 4);//copy mark
            Buffer.BlockCopy(groupLengthData, 0, messageData, 4, 4); //copy group length
            Buffer.BlockCopy(groupDesData, 0, messageData, 8, groupDesData.Length); //copy group name description
            Buffer.BlockCopy(realMessageData.Array, realMessageData.Offset, messageData, 8 + groupDesData.Length, realMessageData.Count);//copy real message data
            return messageData;
        }

        public static bool TryParse(ArraySegment<byte> messageRawData, out GroupTransmitMessage outMessage)
        {
            //| mark(4bytes) |  group length(4bytes) | group names | real message data |
            try
            {
                if (messageRawData.Count < 10)
                {
                    outMessage = null;
                    return false;
                }

                UInt32 transMessageMark = BitConverter.ToUInt32(messageRawData.Array, messageRawData.Offset);

                int groupDesLen = BitConverter.ToInt32(messageRawData.Array, messageRawData.Offset + 4);
                if (groupDesLen > TcpUtility.MaxGroupDesLength)
                {
                    outMessage = null;
                    return false;
                }
                string groupNameDes = Encoding.UTF8.GetString(messageRawData.Array, messageRawData.Offset + 8, groupDesLen);
                if (string.IsNullOrWhiteSpace(groupNameDes))
                {
                    outMessage = null;
                    return false;
                }
                string[] groupArray = groupNameDes.Split('|');
                IEnumerable<string> groupList = groupArray.Where(p => p.Trim() != "");
                if (!groupList.Any())
                {
                    outMessage = null;
                    return false;
                }
                GroupTransmitMessage groupMessage = new GroupTransmitMessage()
                {
                    GroupTransmitMessageMark = transMessageMark,
                    GroupNameCollection = groupList,
                    TransMessageData = new ArraySegment<byte>(messageRawData.Array, messageRawData.Offset + 8 + groupDesLen, messageRawData.Count - 8 - groupDesLen)
                };
                
                outMessage = groupMessage;
                return true;
            }
            catch
            {
                outMessage = null;
                return false;
            }
        }
    }
}
