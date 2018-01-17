using Dapper;
using ElateTableFramework.Configuration;
using ElateTableFramework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace TestApplication.Models
{
    public class AutoRepository : IElateTableRepository<Auto>
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        public List<Auto> GetUsers()
        {
            List<Auto> auto = new List<Auto>();
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                auto = db.Query<Auto>("SELECT * FROM Autos").ToList();
            }
            return auto;
        }

        public IEnumerable<Auto> GetPagination(PaginationConfig config)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return db.GetPagination<Auto>(config);
            }       
        }

        public string GetIndexerJsonArray(PaginationConfig config, string fieldName = null)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return db.GetIndexerJsonArray<Auto>(config, fieldName);
            }
        }

        public IEnumerable<Auto> GetUsersPagination(OrderType type, string col)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return db.GetPagination<Auto>(type, col);
            }
        }

        public void Delete(int index)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                db.Delete<Auto>(index);
            }
        }

        public void Edit(Auto auto)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                db.Update(auto);
            }
        }

        public IEnumerable<string> GetUniqueItems(string field)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return db.GetUniqueItems<Auto>(field);
            }
        }
    }
}