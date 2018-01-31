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
        public List<Auto> GetAutos()
        {
            List<Auto> auto = new List<Auto>();
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                auto = db.Query<Auto>("SELECT * FROM Autos").ToList();
            }
            return auto;
        }

        public void AddAuto(Auto auto)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                db.Insert(auto);
            }
        }

        public IEnumerable<Auto> GetDataWithPagination(ConditionsConfig config, 
                                                       TypeJoinConfiguration joinConfig = null)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return db.GetDataWithPagination<Auto>(config);
            }       
        }

        public string GetIndexerJsonArray(ConditionsConfig config, string fieldName)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return db.GetIndexerJsonArray<Auto>(config, fieldName);
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