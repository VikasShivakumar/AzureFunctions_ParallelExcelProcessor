using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ParallelExcelProcessor
{
    public static class Helpers
    {
        public static async Task WriteToFile(DataTable dataTable)
        {
            using (StreamWriter file = File.CreateText(Path.GetTempPath() + $"\\Sheet{Guid.NewGuid()}.txt"))
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    await file.WriteLineAsync(string.Join("     ", row.ItemArray));
                }
            }
        }

        public static async Task WriteToSQL(DataTable dataTable, IConfiguration configuration)
        {
            SqlConnectionFactory connectionFactory = new SqlConnectionFactory();
            using (SqlConnection connection = connectionFactory.GetSqlConnection(configuration))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("dbo.upsertalldatatypes_procedure", connection);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlParameter tvparam = cmd.Parameters.AddWithValue("@AllDataTypesTable", dataTable);
                tvparam.SqlDbType = SqlDbType.Structured;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public static Stream GenerateStreamFromBytes(byte[] b)
        {
            MemoryStream stream = new MemoryStream(b);
            return stream;
        }
    }
}
