using System; using System.Collections.Generic; using System.IO;
using Microsoft.Data.Sqlite; using SupressIt.Models;
namespace SupressIt.Services
{
    public class BlacklistService : IBlacklistService
    {
        readonly string _conn;
        public BlacklistService(){
            string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"SupressIt");
            Directory.CreateDirectory(dir); _conn=$"Data Source={Path.Combine(dir,"blacklist.db")}"; Init();
        }
        void Init(){using var c=Open();Exec(c,"CREATE TABLE IF NOT EXISTS Blacklist(Id INTEGER PRIMARY KEY AUTOINCREMENT,Name TEXT NOT NULL,EntryType INTEGER NOT NULL,AddedAt TEXT NOT NULL,IsActive INTEGER NOT NULL DEFAULT 1);CREATE UNIQUE INDEX IF NOT EXISTS ix ON Blacklist(Name,EntryType);");}
        public List<BlacklistEntry> GetAll(){
            var l=new List<BlacklistEntry>();using var c=Open();using var cmd=c.CreateCommand();
            cmd.CommandText="SELECT Id,Name,EntryType,AddedAt,IsActive FROM Blacklist ORDER BY AddedAt DESC";
            using var r=cmd.ExecuteReader();while(r.Read())l.Add(new BlacklistEntry{Id=r.GetInt32(0),Name=r.GetString(1),EntryType=(BlacklistType)r.GetInt32(2),AddedAt=r.GetString(3),IsActive=r.GetInt32(4)==1});
            return l;
        }
        public BlacklistEntry? Add(string name,BlacklistType type){
            using var c=Open();
            using var i=c.CreateCommand();i.CommandText="INSERT OR IGNORE INTO Blacklist(Name,EntryType,AddedAt,IsActive)VALUES($n,$t,$a,1)";
            i.Parameters.AddWithValue("$n",name);i.Parameters.AddWithValue("$t",(int)type);i.Parameters.AddWithValue("$a",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));i.ExecuteNonQuery();
            using var s=c.CreateCommand();s.CommandText="SELECT Id,Name,EntryType,AddedAt,IsActive FROM Blacklist WHERE Name=$n AND EntryType=$t";
            s.Parameters.AddWithValue("$n",name);s.Parameters.AddWithValue("$t",(int)type);
            using var r=s.ExecuteReader();if(!r.Read())return null;
            return new BlacklistEntry{Id=r.GetInt32(0),Name=r.GetString(1),EntryType=(BlacklistType)r.GetInt32(2),AddedAt=r.GetString(3),IsActive=r.GetInt32(4)==1};
        }
        public void Remove(int id){using var c=Open();var cmd=c.CreateCommand();cmd.CommandText="DELETE FROM Blacklist WHERE Id=$id";cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery();}
        public void SetActive(int id,bool a){using var c=Open();var cmd=c.CreateCommand();cmd.CommandText="UPDATE Blacklist SET IsActive=$a WHERE Id=$id";cmd.Parameters.AddWithValue("$a",a?1:0);cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery();}
        public bool IsBlacklisted(string n,BlacklistType t){using var c=Open();var cmd=c.CreateCommand();cmd.CommandText="SELECT COUNT(1) FROM Blacklist WHERE Name=$n AND EntryType=$t AND IsActive=1";cmd.Parameters.AddWithValue("$n",n);cmd.Parameters.AddWithValue("$t",(int)t);return(long)cmd.ExecuteScalar()>0;}
        SqliteConnection Open(){var c=new SqliteConnection(_conn);c.Open();return c;}
        static void Exec(SqliteConnection c,string sql){using var cmd=c.CreateCommand();cmd.CommandText=sql;cmd.ExecuteNonQuery();}
    }
}
