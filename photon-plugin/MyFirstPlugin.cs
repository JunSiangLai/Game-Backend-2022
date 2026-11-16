using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.Hive.Plugin;
using System.Data;
using System.Reflection;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace MyFirstPlugin
{
    public class MyFirstPlugin : PluginBase
    {
        public override string Name => "MyFirstPlugin";

        private IPluginLogger pluginLogger;
        Database db = new Database();
        Dictionary<int, string> Usernames = new Dictionary<int,string>();
        Dictionary<int, List<string>> FBUsernames = new Dictionary<int, List<string>>();

        public MyFirstPlugin()
        {
            db.Connect("127.0.0.1", 3306, "serverdev", "root", "YOUR_DB_PASSWORD" /* replace with your MySQL password, see README */);
        }

        public override bool SetupInstance(IPluginHost host, Dictionary<string, string> config, out string errorMsg)
        {
            this.pluginLogger = host.CreateLogger(this.Name);
            return base.SetupInstance(host, config, out errorMsg);
        }

        public override void OnCreateGame(ICreateGameCallInfo info)
        {
            this.pluginLogger.InfoFormat("OnCreateGame {0} by user {1}", info.Request.GameId, info.UserId);
            info.Continue(); // same as base.OnCreateGame(info);
        }

        public override void OnLeave(ILeaveGameCallInfo info)
        {
            base.OnLeave(info);
            Usernames.Remove(info.ActorNr);
        }

        public override void OnRaiseEvent(IRaiseEventCallInfo info)
        {
            base.OnRaiseEvent(info);
            switch (info.Request.EvCode)
            {
                case 1: // Disconnect
                    {
                        if (Usernames.ContainsKey(info.ActorNr))
                            Usernames.Remove(info.ActorNr);
                        else
                            FBUsernames.Remove(info.ActorNr);

                        break;
                    }
                case 2: // Log In
                    {
                        string[] UDcredentials = Encoding.Default.GetString((byte[])info.Request.Data).Split(',');
                        DataTable Scredentials = db.Query($"SELECT * FROM server WHERE username = '{UDcredentials[0]}' && is_facebook = '0'");
                        // Username not found
                        if (Scredentials == null || Scredentials.Rows.Count == 0)
                        {
                            this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { info.ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, false },
                            { (byte)244, string.Format("{0}", JsonConvert.SerializeObject("User does not exist")) } },
                                evCode: info.Request.EvCode,
                                cacheOp: 0);
                        }
                        // Logged In
                        else if (Scredentials.Rows[0].ItemArray[1].ToString().Equals(UDcredentials[1]))
                        {
                            // Logged In elsewhere
                            if (Usernames.ContainsValue(UDcredentials[0]))
                            {
                                int ActorNr = Usernames.FirstOrDefault(x => x.Value == UDcredentials[1]).Key;
                                this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, string.Format("{0}", JsonConvert.SerializeObject("Disconnect")) } },
                                evCode: 9,
                                cacheOp: 0);
                                Usernames.Remove(ActorNr);
                            }
                            // Success
                            this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { info.ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, true } },
                                evCode: info.Request.EvCode,
                                cacheOp: 0);
                            Usernames.Add(info.ActorNr, UDcredentials[0]);
                        }
                        // Password not found
                        else
                        {
                            this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { info.ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, false },
                            { (byte)244, string.Format("{0}", JsonConvert.SerializeObject("Password is incorrect")) } },
                                evCode: info.Request.EvCode,
                                cacheOp: 0);
                        }
                        break;
                    }
                case 3: // Sign Up
                    {
                        string[] UDcredentials = Encoding.Default.GetString((byte[])info.Request.Data).Split(',');
                        if (!db.NonQuery($"INSERT INTO server(username, password) VALUES('{UDcredentials[0]}', '{UDcredentials[1]}')", out string error))
                        {
                            this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { info.ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, string.Format("{0}", JsonConvert.SerializeObject(error)) } },
                                evCode: info.Request.EvCode,
                                cacheOp: 0);
                        }
                        else
                        {
                            PostDiscJsonRequest(Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL"), UDcredentials[0], info, false);
                            this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { info.ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, true } },
                                evCode: 2,
                                cacheOp: 0);
                        }
                        break;
                    }
                case 4: // Add to Inventory
                    {
                        if (Usernames.TryGetValue(info.ActorNr, out string username))
                        {
                            string[] values = Encoding.Default.GetString((byte[])info.Request.Data).Split(',');
                            if (!db.NonQuery($"INSERT INTO user(username, item_name, is_facebook, count, ID) VALUES ('{username}', '{values[0]}', '0', '{values[1]}', uuid())" +
                                $"ON DUPLICATE KEY UPDATE count = count + {values[1]};", out string error))
                            {
                                this.PluginHost.BroadcastEvent(
                                    recieverActors: new List<int>() { info.ActorNr },
                                    senderActor: 0,
                                    data: new Dictionary<byte, object>() { { (byte)245, "Error when adding" },
                                        { (byte)244, false } },
                                    evCode: info.Request.EvCode,
                                    cacheOp: 0);
                            }
                            else
                            {
                                DataTable dt = db.Query($"SELECT * FROM user WHERE username = '{username}' && item_name = '{values[0]}' && is_facebook = 0");
                                List<User> Users = Database.DataTableToList<User>(dt);
                                string response = string.Format("{0}",
                                JsonConvert.SerializeObject(Users));
                                this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { info.ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, response },
                                    { (byte)244, true } },
                                evCode: info.Request.EvCode,
                                cacheOp: 0);
                            }
                        }
                        else if (FBUsernames.TryGetValue(info.ActorNr, out List<string> FBuserCode))
                        {
                            string[] values = Encoding.Default.GetString((byte[])info.Request.Data).Split(',');
                            if (!db.NonQuery($"INSERT INTO user(username, item_name, is_facebook, count, ID) VALUES ('{FBuserCode[0]}', '{values[0]}', '{FBuserCode[1]}', '{values[1]}', uuid())" +
                                $"ON DUPLICATE KEY UPDATE count=count+{values[1]};", out string error))
                            {
                                this.PluginHost.BroadcastEvent(
                                    recieverActors: new List<int>() { info.ActorNr },
                                    senderActor: 0,
                                    data: new Dictionary<byte, object>() { { (byte)245, "Error when adding" },
                                        { (byte)244, false } },
                                    evCode: info.Request.EvCode,
                                    cacheOp: 0);
                            }
                            else
                            {
                                DataTable dt = db.Query($"SELECT * FROM user WHERE username = '{FBuserCode[0]}' && item_name = '{values[0]}' && is_facebook = {FBuserCode[1]}");
                                List<User> Users = Database.DataTableToList<User>(dt);
                                string response = string.Format("{0}",
                                JsonConvert.SerializeObject(Users));
                                this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { info.ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, response },
                                    { (byte)244, true } },
                                evCode: info.Request.EvCode,
                                cacheOp: 0);
                            }
                        }
                        else
                        {
                            this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { info.ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, "Error: Unregisted User" },
                                    { (byte)244, false } },
                                evCode: info.Request.EvCode,
                                cacheOp: 0);
                        }
                        break;
                    }
                case 5: // Remove Inventory
                    {
                        string itemID = Encoding.Default.GetString((byte[])info.Request.Data);
                        DataTable dt = db.Query($"SELECT * FROM user WHERE ID = '{itemID}'");
                        List<User> Users = Database.DataTableToList<User>(dt);
                        if (Users.Count == 0)
                        {
                            this.PluginHost.BroadcastEvent(
                            recieverActors: new List<int>() { info.ActorNr },
                            senderActor: 0,
                            data: new Dictionary<byte, object>() { { (byte)245, "Error removing" },
                                    { (byte)244, false } },
                            evCode: info.Request.EvCode,
                            cacheOp: 0);
                            break;
                        }
                        if (Usernames.TryGetValue(info.ActorNr, out string username))
                        {
                            if (Users[0].username != username)
                            {
                                break;
                            }
                        }
                        else if (FBUsernames.TryGetValue(info.ActorNr, out List<string> FBuserCode))
                        {
                            if (Users[0].username != FBuserCode[0])
                            {
                                break;
                            }
                        }
                        if (Users[0].count == 1)
                        {
                            if (!db.NonQuery($"DELETE FROM user WHERE ID = '{Users[0].ID}'", out string error))
                            {
                                this.PluginHost.BroadcastEvent(
                                    recieverActors: new List<int>() { info.ActorNr },
                                    senderActor: 0,
                                    data: new Dictionary<byte, object>() { { (byte)245, "Error removing" },
                                        { (byte)244, false } },
                                    evCode: info.Request.EvCode,
                                    cacheOp: 0);
                            }

                            dt = db.Query($"SELECT * FROM user WHERE ID = '{itemID}'");
                            Users = Database.DataTableToList<User>(dt);
                            string response = string.Format("{0}",
                            JsonConvert.SerializeObject(Users));
                            this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { info.ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, "" },
                                    { (byte)244, true },
                                    { (byte)243, itemID } },
                                evCode: info.Request.EvCode,
                                cacheOp: 0);
                        }
                        else
                        {
                            if (!db.NonQuery($"UPDATE user SET count = count - 1 WHERE ID = '{Users[0].ID}';", out string error))
                            {
                                this.PluginHost.BroadcastEvent(
                                    recieverActors: new List<int>() { info.ActorNr },
                                    senderActor: 0,
                                    data: new Dictionary<byte, object>() { { (byte)245, "Error removing" },
                                        { (byte)244, false } },
                                    evCode: info.Request.EvCode,
                                    cacheOp: 0);
                            }

                            dt = db.Query($"SELECT * FROM user WHERE ID = '{itemID}'");
                            Users = Database.DataTableToList<User>(dt);
                            string response = string.Format("{0}",
                            JsonConvert.SerializeObject(Users));
                            this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { info.ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, response },
                                    { (byte)244, true } },
                                evCode: info.Request.EvCode,
                                cacheOp: 0);
                        }
                        break;
                    }
                case 6: // Update Inventory
                    {
                        string[] filterSettings = Encoding.Default.GetString((byte[])info.Request.Data).Split(',');
                        int filter = 0;
                        switch (filterSettings[1])
                        {
                            case "None":
                                filter = 0;
                                break;
                            case "Most Recent":
                                filter = 1;
                                break;
                            case "Alphabet":
                                filter = 2;
                                break;
                            case "User":
                                filter = 3;
                                break;
                        }
                        if (filterSettings[0] == "true")
                            QueryUserTable("user", filter, info);
                        else
                            QueryTable("user", filter, info);
                        break;
                    }
                case 7: // Facebook
                    {
                        string[] UDcredentials = Encoding.Default.GetString((byte[])info.Request.Data).Split(',');
                        DataTable Scredentials = db.Query($"SELECT * FROM server WHERE username = '{UDcredentials[0]}' && is_facebook = '{UDcredentials[1]}'");
                        if (FBUsernames.ContainsValue(new List<string>() { UDcredentials[0], UDcredentials[1] }))
                        {
                            int ActorNr = FBUsernames.FirstOrDefault(x => x.Value == new List<string>() { UDcredentials[0], UDcredentials[1] }).Key;
                            this.PluginHost.BroadcastEvent(
                                    recieverActors: new List<int>() { ActorNr },
                                    senderActor: 0,
                                    data: new Dictionary<byte, object>() { { (byte)245, true }, 
                                        { (byte)244, string.Format("{0}", JsonConvert.SerializeObject("Disconnect")) } },
                                    evCode: 9,
                                    cacheOp: 0);
                        }
                        // New
                        if (Scredentials == null || Scredentials.Rows.Count == 0)
                        {
                            // Add
                            if (!db.NonQuery($"INSERT INTO server(username, is_facebook) VALUES('{UDcredentials[0]}', '{UDcredentials[1]}')", out string error))
                            {
                                this.PluginHost.BroadcastEvent(
                                    recieverActors: new List<int>() { info.ActorNr },
                                    senderActor: 0,
                                    data: new Dictionary<byte, object>() { { (byte)245, false },
                            { (byte)244, string.Format("{0}", JsonConvert.SerializeObject(error)) } },
                                    evCode: info.Request.EvCode,
                                    cacheOp: 0);
                                break;
                            }
                        }
                        // Exist so LogIn
                        this.PluginHost.BroadcastEvent(
                                    recieverActors: new List<int>() { info.ActorNr },
                                    senderActor: 0,
                                    data: new Dictionary<byte, object>() { { (byte)245, true } },
                                    evCode: info.Request.EvCode,
                                    cacheOp: 0);
                        FBUsernames.Add(info.ActorNr, new List<string>() { UDcredentials[0], UDcredentials[1] });
                        break;
                    }
                case 8: // PassReset
                    {
                        string[] UDcredentials = Encoding.Default.GetString((byte[])info.Request.Data).Split(',');
                        DataTable Scredentials = db.Query($"SELECT * FROM server WHERE username = '{UDcredentials[0]}' && is_facebook = '0'");
                        // Username not found
                        if (Scredentials == null || Scredentials.Rows.Count == 0)
                        {
                            this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { info.ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, false },
                            { (byte)244, string.Format("{0}", JsonConvert.SerializeObject("User does not exist")) } },
                                evCode: info.Request.EvCode,
                                cacheOp: 0);
                        }
                        else
                        {
                            if (db.NonQuery($"UPDATE server SET password = '{UDcredentials[1]}' WHERE username = '{UDcredentials[0]}' && is_facebook = '0';", out string error))
                            {
                                this.PluginHost.BroadcastEvent(
                                recieverActors: new List<int>() { info.ActorNr },
                                senderActor: 0,
                                data: new Dictionary<byte, object>() { { (byte)245, true } },
                                evCode: info.Request.EvCode,
                                cacheOp: 0);
                            }
                        }
                        break;
                    }
            }
        }

        private void PostDiscJsonRequest(string url, string content, ICallInfo info, bool callAsync = false)
        {
            var stream = new MemoryStream();
            var json = JsonConvert.SerializeObject(new
            {
                username = "Server Bot",
                content = "",
                embeds = new[]
                {
                    new
                    {
                        title = "New player registered",
                        description = "Welcome " + content + "!",
                        color = "65331"
                    }
                }
            });
            var data = Encoding.UTF8.GetBytes(json);
            stream.Write(data, 0, data.Length);

            var request = new HttpRequest
            {
                Url = url,
                Method = "POST",
                Accept = "application/json",
                Headers = new Dictionary<HttpRequestHeader, string>() { { HttpRequestHeader.ContentType, "application/json" } },
                DataStream = stream,
                Async = callAsync
            };

            this.PluginHost.LogDebug(string.Format("PostJsonRequest: {0} - {1}", url, json));

            this.PluginHost.HttpRequest(request, info);
        }

        public void QueryTable(string table, int filter, IRaiseEventCallInfo info)
        {
            string filterCommand = "";
            switch (filter)
            {
                case 1:
                    filterCommand = " ORDER BY upload_time DESC;";
                    break;
                case 2:
                    filterCommand = " ORDER BY item_name;";
                    break;
                case 3:
                    filterCommand = " ORDER BY username;";
                    break;
            }
            DataTable dt = db.Query($"SELECT * FROM {table}" + filterCommand);
            List<User> Users = Database.DataTableToList<User>(dt);
            string response = string.Format("{0}",
            JsonConvert.SerializeObject(Users));
            this.PluginHost.BroadcastEvent(
            recieverActors: new List<int>() { info.ActorNr },
            senderActor: 0,
            data: new Dictionary<byte, object>() { { (byte)245, response } },
            evCode: info.Request.EvCode,
            cacheOp: 0);
        }

        public void QueryUserTable(string table, int filter, IRaiseEventCallInfo info)
        {
            string filterCommand = "";
            switch (filter)
            {
                case 1:
                    filterCommand = " ORDER BY upload_time DESC;";
                    break;
                case 2:
                    filterCommand = " ORDER BY item_name;";
                    break;
                case 3:
                    filterCommand = " ORDER BY username;";
                    break;
            }
            if (Usernames.TryGetValue(info.ActorNr, out string username))
            {
                DataTable dt = db.Query($"SELECT * FROM {table} WHERE username = '{username}' && is_facebook = '0'" + filterCommand);
                List<User> Users = Database.DataTableToList<User>(dt);
                string response = string.Format("{0}",
                JsonConvert.SerializeObject(Users));
                this.PluginHost.BroadcastEvent(
                recieverActors: new List<int>() { info.ActorNr },
                senderActor: 0,
                data: new Dictionary<byte, object>() { { (byte)245, response } },
                evCode: info.Request.EvCode,
                cacheOp: 0);
            }
            else if (FBUsernames.TryGetValue(info.ActorNr, out List<string> FBuserCode))
            {
                DataTable dt = db.Query($"SELECT * FROM {table} WHERE username = '{FBuserCode[0]}' && is_facebook = '{FBuserCode[1]}'" + filterCommand);
                List<User> Users = Database.DataTableToList<User>(dt);
                string response = string.Format("{0}",
                JsonConvert.SerializeObject(Users));
                this.PluginHost.BroadcastEvent(
                recieverActors: new List<int>() { info.ActorNr },
                senderActor: 0,
                data: new Dictionary<byte, object>() { { (byte)245, response } },
                evCode: info.Request.EvCode,
                cacheOp: 0);
            }
        }
    }

    public class User
    {
        public string username { get; set; }
        public string item_name { get; set; }
        public uint count { get; set; }
        public string ID { get; set; }
        public string is_facebook { get; set; }
    }
}
