using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiS.Communication.Tcp
{
    /// <summary>
    /// The tcp message for join specific groups.
    /// </summary>
    internal class JoinGroupMessage
    {
        public UInt32 JoinGroupMark { get; set; }
        public HashSet<string> GroupSet { get; set; }

        public static byte[] MakeMessage(IEnumerable<string> groupNameCollection)
        {
            //| mark(4bytes) | group names |
            string groupNameDes = "";
            foreach (string groupName in groupNameCollection)
            {
                groupNameDes += groupName.Replace("|", "") + "|";
            }
            groupNameDes = groupNameDes.TrimEnd('|');
            byte[] markHeaderData = BitConverter.GetBytes(TcpUtility.JOIN_GROUP_MARK);
            byte[] groupDesData = Encoding.UTF8.GetBytes(groupNameDes);
            byte[] messageData = new byte[4 + groupDesData.Length];
            Buffer.BlockCopy(markHeaderData, 0, messageData, 0, 4);
            Buffer.BlockCopy(groupDesData, 0, messageData, 4, groupDesData.Length);
            return messageData;
        }
        public static bool TryParse(ArraySegment<byte> messageRawData, out JoinGroupMessage outMessage)
        {
            //| mark(4bytes) | group names |
            try
            {
                if (messageRawData.Count <= 4)
                {
                    outMessage = null;
                    return false;
                }
                UInt32 joinGroupMark = BitConverter.ToUInt32(messageRawData.Array, messageRawData.Offset);
                string groupNameDes = Encoding.UTF8.GetString(messageRawData.Array, messageRawData.Offset + 4, messageRawData.Count - 4);
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
                JoinGroupMessage joinGroupMsg = new JoinGroupMessage()
                {
                    JoinGroupMark = joinGroupMark,
                    GroupSet = new HashSet<string>(groupList)
                };
                outMessage = joinGroupMsg;
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
