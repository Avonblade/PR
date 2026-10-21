using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;
namespace SchoolApp;
public static class Db
{
 public static SqlConnection Open(){var c=new SqlConnection(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"connection.txt")).Trim());c.Open();return c;}
 public static DataTable Query(string sql, params (string,object?)[] args){using var c=Open();using var cmd=new SqlCommand(sql,c);Bind(cmd,args);using var r=cmd.ExecuteReader();var t=new DataTable();t.Load(r);return t;}
 public static void Bind(SqlCommand cmd,params (string,object?)[] args){foreach(var (n,v) in args)cmd.Parameters.AddWithValue(n,v??DBNull.Value);}
 public static void Execute(string sql,params (string,object?)[] args){using var c=Open();using var cmd=new SqlCommand(sql,c);Bind(cmd,args);cmd.ExecuteNonQuery();}
 public static void Delete(int id){using var c=Open();using var tx=c.BeginTransaction();using var cmd=new SqlCommand("IF EXISTS(SELECT 1 FROM ClientService WITH(UPDLOCK,HOLDLOCK) WHERE ServiceID=@id) THROW 50001,N'Удаление запрещено: на услугу есть записи.',1; DELETE ServicePhoto WHERE ServiceID=@id; DELETE Service WHERE ID=@id;",c,tx);cmd.Parameters.AddWithValue("@id",id);cmd.ExecuteNonQuery();tx.Commit();}
 public static int Save(int id,string title,decimal cost,int seconds,string description,decimal discount,string image,IEnumerable<string> photos){using var c=Open();using var tx=c.BeginTransaction();using var cmd=new SqlCommand(id==0?"INSERT Service(Title,Cost,DurationInSeconds,Description,Discount,MainImagePath) OUTPUT INSERTED.ID VALUES(@t,@c,@s,@d,@p,@i)":"UPDATE Service SET Title=@t,Cost=@c,DurationInSeconds=@s,Description=@d,Discount=@p,MainImagePath=@i WHERE ID=@id; SELECT @id;",c,tx);Bind(cmd,("@t",title),("@c",cost),("@s",seconds),("@d",description),("@p",discount),("@i",image),("@id",id));id=Convert.ToInt32(cmd.ExecuteScalar());cmd.Parameters.Clear();cmd.CommandText="DELETE ServicePhoto WHERE ServiceID=@id";Bind(cmd,("@id",id));cmd.ExecuteNonQuery();foreach(var p in photos){cmd.Parameters.Clear();cmd.CommandText="INSERT ServicePhoto(ServiceID,PhotoPath) VALUES(@id,@p)";Bind(cmd,("@id",id),("@p",p));cmd.ExecuteNonQuery();}tx.Commit();return id;}
}
