using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Portfolio.Common
{
    /// <summary>
    /// Lightweight data access layer for SQL Server.
    /// Demonstrates: parameterized queries, IDisposable patterns, dependency injection via IConfiguration.
    /// </summary>
    public static class DataAccess
    {
        private const string ConnectionTemplate = "Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=False;Connect Timeout={2}";

        /// <summary>
        /// Execute a SQL query and return results as DataTable.
        /// </summary>
        public static DataTable ExecSQL(
            IConfiguration configuration,
            string cmdText,
            CommandType cmdType,
            SqlParameter[] parameters = null,
            string connection = "")
        {
            DataTable results = new DataTable();

            if (string.IsNullOrEmpty(connection))
            {
                string server = configuration.GetSection("AppSettings")["Server"];
                string database = configuration.GetSection("AppSettings")["Database"];
                connection = string.Format(ConnectionTemplate, server, database, "15");
            }

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = cmdType;

                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                cmd.CommandText = cmdText;
                cmd.Connection = conn;
                cmd.CommandTimeout = 0;

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    results.Load(reader);
                }
            }

            return results;
        }

        /// <summary>
        /// Execute a non-query command (INSERT, UPDATE, DELETE).
        /// </summary>
        public static string ExecSQLNonQuery(
            IConfiguration configuration,
            string cmdText,
            CommandType cmdType,
            SqlParameter[] parameters = null,
            string connection = "")
        {
            int rowsAffected = 0;
            string returnVal = "";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = cmdType;

                try
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    cmd.CommandText = cmdText;
                    cmd.Connection = conn;
                    cmd.CommandTimeout = 0;

                    conn.Open();
                    rowsAffected = cmd.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    returnVal = e.Message;
                }
            }

            if (returnVal == "")
            {
                string plurality = (rowsAffected != 1) ? "s" : "";
                returnVal = $"Commands completed successfully ({rowsAffected} row{plurality} affected).";
            }

            return returnVal;
        }

        /// <summary>
        /// Execute a query returning multiple result sets.
        /// </summary>
        public static DataSet ExecSQLMulti(
            IConfiguration configuration,
            string cmdText,
            CommandType cmdType,
            SqlParameter[] parameters = null,
            string connection = "")
        {
            DataSet results = new DataSet();

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = cmdType;

                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                cmd.CommandText = cmdText;
                cmd.Connection = conn;
                cmd.CommandTimeout = 0;

                conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(results);
            }

            return results;
        }

        /// <summary>
        /// Execute a query and return results as JSON using SQL Server's FOR JSON AUTO.
        /// </summary>
        public static object ExecJson(IConfiguration configuration, string sql, string connection = "")
        {
            object jsonResult = null;
            StringBuilder jsonResultSB = new StringBuilder();

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand($"{sql} FOR JSON AUTO", conn))
            {
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        jsonResultSB.Append("[]");
                    }
                    else
                    {
                        while (reader.Read())
                        {
                            jsonResultSB.Append(reader.GetValue(0).ToString());
                        }
                    }
                }
            }

            jsonResult = JsonConvert.DeserializeObject(jsonResultSB.ToString());
            return jsonResult;
        }

        /// <summary>
        /// Lookup a single string value from a table.
        /// </summary>
        public static string LookupString(
            IConfiguration configuration,
            string tableOrView,
            string columnName,
            string filter = "",
            string defaultIfEmpty = "")
        {
            string value = defaultIfEmpty;

            string sql = $"SELECT TOP 1 value = CAST({columnName} AS VARCHAR(MAX)) FROM {tableOrView}";
            if (!string.IsNullOrEmpty(filter))
            {
                sql += $" WHERE {filter}";
            }

            DataTable dt = ExecSQL(configuration, sql, CommandType.Text, null);

            if (dt.Rows.Count == 1)
            {
                value = dt.Rows[0]["value"]?.ToString() ?? defaultIfEmpty;
            }

            return value;
        }

        /// <summary>
        /// Lookup a single integer value from a table.
        /// </summary>
        public static int LookupInt(
            IConfiguration configuration,
            string tableOrView,
            string columnName,
            string filter = "",
            int defaultIfEmpty = 0)
        {
            int value = defaultIfEmpty;

            string sql = $"SELECT TOP 1 value = {columnName} FROM {tableOrView}";
            if (!string.IsNullOrEmpty(filter))
            {
                sql += $" WHERE {filter}";
            }

            DataTable dt = ExecSQL(configuration, sql, CommandType.Text, null);

            if (dt.Rows.Count == 1)
            {
                int.TryParse(dt.Rows[0][0]?.ToString(), out value);
            }

            return value;
        }
    }
}
