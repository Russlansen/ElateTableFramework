using Dapper;
using ElateTableFramework;
using ElateTableFramework.Configuration;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace TestApplication.Models
{
    public class UserRepository: IElateTableRepository<User>
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        //public List<User> GetUsers()
        //{
        //    List<User> users = new List<User>();
        //    using (IDbConnection db = new SqlConnection(connectionString))
        //    {
        //        var sql = @"SELECT Users.Id, Users.Name, Users.LastName, Users.Age, Autos.Id, Autos.Model, Autos.Price 
        //                    FROM Users inner join Autos on Users.ModelId = Autos.Id";
        //        users = db.Query<User, Auto, User>(sql, (user, auto) => { user.Auto = auto; return user; }).ToList();
        //    }
        //    return users;
        //}

        public void AddUser(User user)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                db.Insert(user);
            }
        }

        public IEnumerable<User> GetDataWithPagination(ConditionsConfig pagerConfig, 
                                                       TypeJoinConfiguration joinConfig = null)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return db.GetDataWithPagination<User>(pagerConfig, joinConfig);
            }
        }

        public IEnumerable<string> GetUniqueItems(string field)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return db.GetUniqueItems<User>(field);
            }
        }

        public string GetIndexerJsonArray(ConditionsConfig config, string fieldName)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return db.GetIndexerJsonArray<User>(config, fieldName);
            }
        }

        public void Delete(int index)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                db.Delete<User>(index);
            }
        }

        public void Edit(User user, TypeJoinConfiguration joinedTable)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                db.UpdateJoinedData(user, joinedTable);
                //db.Update(user);
            }
        }
    }
}