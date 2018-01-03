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

        public IEnumerable<Auto> GetUsersPagination(PaginationConfig config, out int count)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return db.GetPagination<Auto>(config, out count);
            }       
        }
        
    }
}