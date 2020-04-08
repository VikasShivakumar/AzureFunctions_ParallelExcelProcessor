using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace ParallelExcelProcessor
{
    public class SqlConnectionFactory
    {
        public SqlConnection GetSqlConnection(IConfiguration configuration)
        {
            string sqlConnectionString = configuration["ConnectionStrings:SqlConnectionString"];
            return new SqlConnection(sqlConnectionString);
        }
    }
}
