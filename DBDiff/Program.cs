using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoorMansTSqlFormatterLib;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace DBDiff {
    class Program {
        static void Main(string[] args) {
            //if (args.Length > 0) {
                ExportSql es = new ExportSql();
                //es.CreateFiles(args[0]);
                es.CreateFiles("test");
           // }
        }
    }

    public class ExportSql {

        public void CheckDirectory(string name) {
            DirectoryInfo di = new DirectoryInfo(Environment.CurrentDirectory + "\\Files\\" + name);
            if (di.Exists) {
                di.Delete(true);
            } else {
                di.Create();
            }
        }

        public void ProcessType(SqlConnection conn, string dbName, string sql, string type) {
            SqlCommand command = new SqlCommand();
            command.Connection = conn;
            command.CommandText = sql;

            SqlDataAdapter da = new SqlDataAdapter(command);
            DataTable dt = new DataTable();
            da.Fill(dt); //get tables

            if (dt.Rows.Count > 0) {
                
                CheckDirectory(dbName + "\\" + type);

                foreach (DataRow dr in dt.Rows) {
                    string name = dr["name"].ToString();
                    string[] names = name.Split('.');

                    command = new SqlCommand();
                    command.Connection = conn;
                    command.CommandType = CommandType.StoredProcedure;

                    string colName = "text";

                    switch (type) {
                        case "Tables":
                            command.CommandText = "dbo.sp_helptable";
                            command.Parameters.AddWithValue("@tableName", names[1]);
                            command.Parameters.AddWithValue("@schemaName", names[0]);
                            command.Parameters.AddWithValue("@includeDBName", false);
                            colName = "TableScript";
                            break;
                        case "Views":
                        case "Procedures":
                            command.CommandText = "sp_helptext";
                            command.Parameters.AddWithValue("@objname", name);
                            colName = "Text";
                            break;
                    }

                    DataTable dtD = new DataTable();
                    da = new SqlDataAdapter(command);
                    da.Fill(dtD);
                    if (dtD.Rows.Count > 0) {
                        StringBuilder sb = new StringBuilder();
                        foreach (DataRow drD in dtD.Rows) {
                            sb.Append(drD[colName]);
                        }

                        string sqlScript = FormatSql(sb.ToString()); //format sql
                        File.WriteAllText(Environment.CurrentDirectory + "\\Files\\" + dbName + "\\"+ type +"\\" + name + ".sql", sqlScript);
                    }
                }
            };
        }

        public void CheckSpHelp(SqlConnection conn) {
            SqlCommand command = new SqlCommand(Resource1.sp_helptable_exists, conn);
            SqlDataAdapter da = new SqlDataAdapter(command);
            DataTable dt = new DataTable();
            da.Fill(dt);
            if (dt.Rows.Count > 0) {
                DataRow dr = dt.Rows[0];
                int sprocExists = Convert.ToInt32(dr["sprocExists"]);
                if (sprocExists == 0) {
                    command.CommandText = Resource1.sp_helptable;
                    command.ExecuteNonQuery();
                }
            }
        }

        public void CreateFiles(string dbName) {
            string connectionString = Resource1.ConnectionString.Replace("dbName", dbName);
            using (SqlConnection conn = new SqlConnection(connectionString)) {
                conn.Open();

                CheckSpHelp(conn);
                CheckDirectory(dbName); //create directories

                ProcessType(conn, dbName, Resource1.Tables, "Tables");
                ProcessType(conn, dbName, Resource1.Procedures, "Procedures");
                ProcessType(conn, dbName, Resource1.Views, "Views");
                conn.Close();
            }
        }

        public string FormatSql(string sql) {
            var formatter = new PoorMansTSqlFormatterLib.Formatters.TSqlStandardFormatter {
                IndentString = "\t",
                SpacesPerTab = 4,
                MaxLineWidth = 999,
                TrailingCommas = false,
                SpaceAfterExpandedComma = false,
                ExpandBetweenConditions = true,
                ExpandBooleanExpressions = true,
                ExpandCaseStatements = true,
                ExpandCommaLists = true,
                BreakJoinOnSections = false,
                UppercaseKeywords = true
            };
            var formattingManager = new PoorMansTSqlFormatterLib.SqlFormattingManager(formatter);
            return formattingManager.Format(sql);
        }
    }
}
