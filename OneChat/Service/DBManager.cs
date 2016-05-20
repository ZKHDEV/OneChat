using OneChat.Model;
using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneChat.Service
{
    public class DBManager
    {
        private static readonly SQLiteConnection con = DBContext.GetSQLConnection();

        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static int InsertMsg(MessageInfo msg)
        {
            db_Message dbmsg = new db_Message
            {
                Name = msg.Name,
                Target = msg.Target,
                Message = msg.Message,
                Bmp = msg.Bmp,
                Time = msg.Time
            };

            var count = con.Insert(dbmsg);
            return count;
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static int DeleteMsg()
        {
            var count = con.DeleteAll<db_Message>();
            return count;
        }

        /// <summary>
        /// 查看所有数据
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static int NotifyMsg(ObservableCollection<MessageInfo> msglist)
        {
            var MsgList = con.Table<db_Message>().ToList();
            msglist.Clear();
            MsgList.ForEach(m =>
            {
                MessageInfo msg = new MessageInfo
                {
                    Name = m.Name,
                    Target = m.Target,
                    Message = m.Message,
                    Bmp = m.Bmp,
                    Time = m.Time
                };
                msglist.Add(msg);
            });
            return MsgList.Count();
        }

        /// <summary>
        /// 查看客户端所有数据
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static int NotifyMsg(ObservableCollection<MessageInfo> msglist, string name)
        {
            var MsgList = con.Table<db_Message>().ToList();
            msglist.Clear();
            if (MsgList.Any(m => m.Name == name || m.Target == name))
            {
                MsgList.Where(m => m.Name == name || m.Target == name).ToList().
                    ForEach(m =>
                    {
                        MessageInfo msg = new MessageInfo
                        {
                            Name = m.Name,
                            Target = m.Target,
                            Message = m.Message,
                            Bmp = m.Bmp,
                            Time = m.Time
                        };
                        msglist.Add(msg);
                    });
            }
            return MsgList.Count();
        }
    }
}
