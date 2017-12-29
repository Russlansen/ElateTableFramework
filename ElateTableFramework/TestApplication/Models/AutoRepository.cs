using Dapper;
using ElateTableFramework.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace TestApplication.Models
{
    public class AutoRepository
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

        public List<Auto> GetUsersPagination(string orderBy, string orderType, int page, PaginationConfig config, out int count)
        {
            var queryString = $"SELECT * FROM Autos ORDER BY {orderBy} {orderType} OFFSET " +
                              $"{config.Offset}" +
                              $" ROWS FETCH NEXT {config.MaxItemsInPage} ROWS ONLY";
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                count = db.Query<int>($"SELECT COUNT (*) FROM Autos").FirstOrDefault();
                return db.Query<Auto>(queryString).ToList();
            }
                
        }
        
    }
}