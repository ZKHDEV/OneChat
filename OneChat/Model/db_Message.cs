using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneChat.Model
{
    public class db_Message
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { set; get; }

        public string Message { set; get; }
        public byte[] Bmp { set; get; }
        public string Name { set; get; }
        public string Target { set; get; }
        public DateTime Time { set; get; }
    }
}
