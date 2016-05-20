using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneChat.Model
{
    public class MessageInfo
    {
        public MessageType MsgType { set; get; }
        public string Message { set; get; }
        public byte[] Bmp { set; get; }
        public string Name { set; get; }
        public string Target { set; get; }
        public List<string> OnlineUser { set; get; }
        public DateTime Time { set; get; }
    }

    public enum MessageType
    {
        Disconnection,
        TextMessage,
        Update
    }
}
