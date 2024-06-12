using JudgeSearcher.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace JudgeSearcher.Utility
{
    public static class Database
    {

        static string connectionString = string.Format("Data Source={0}", "Judges.db");

        public static async Task<bool> Drop()
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                SqliteCommand command = connection.CreateCommand();

                command.CommandText = @"DROP TABLE IF EXISTS Judges;";

                command.ExecuteNonQuery();
            }

            return false;
        }

        public static async Task<bool> Create()
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                using (SqliteTransaction transaction = connection.BeginTransaction())
                {
                    SqliteCommand command = connection.CreateCommand();

                    command.CommandText = @"CREATE TABLE IF NOT EXISTS Judges (
                        ID TEXT,
                        Type TEXT,
                        FirstName TEXT,
                        LastName TEXT,
                        JudicialAssistant TEXT,
                        Phone TEXT,
                        Location TEXT,
                        Street TEXT,
                        City TEXT,
                        Zip TEXT,
                        County TEXT,
                        Circuit TEXT,
                        District TEXT,
                        CourtRoom TEXT,
                        HearingRoom TEXT,
                        SubDivision TEXT
                    );";

                    command.ExecuteNonQuery();

                    transaction.Commit();
                }
            }

            return false;
        }

        public static async Task<bool> Delete(string circuit)
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                using (SqliteTransaction transaction = connection.BeginTransaction())
                {
                    SqliteCommand command = connection.CreateCommand();

                    var sql = circuit.Equals("All") ? "DELETE FROM Judges;" : string.Format("DELETE FROM Judges WHERE Circuit = '{0}';", circuit);

                    command.CommandText = sql;

                    command.ExecuteNonQuery();

                    transaction.Commit();
                }
            }

            return false;
        }

        public static async Task<bool> Batch(DataTable table)
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                using (SqliteTransaction transaction = connection.BeginTransaction())
                {
                    List<string> queries = new List<string>();

                    foreach (DataRow row in table.Rows)
                    {
                        List<string> fields = new List<string>();

                        for (int i = 0; i < table.Columns.Count; i++)
                        {
                            fields.Add(string.Format("'{0}' AS '{1}'", row[i].ToString().Replace("'", "''"), table.Columns[i].ColumnName));
                        }

                        queries.Add(string.Format("SELECT {0}\r\n", string.Join(", ", fields)));
                    }

                    string sql = string.Format("INSERT INTO Judges {0};", string.Join("UNION ALL ", queries));

                    SqliteCommand command = connection.CreateCommand();

                    command.CommandText = sql;

                    command.ExecuteNonQuery();

                    transaction.Commit();
                }
            }

            return true;
        }


        public static List<Judge> Select(string circuit)
        {
            List<Judge> collection = new List<Judge>();

            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var sql = circuit.Equals("All") ? @"SELECT ID, Type, FirstName, LastName, JudicialAssistant, Phone, Location, Street, City, Zip, County, Circuit, District, CourtRoom, HearingRoom, SubDivision FROM Judges;" : string.Format("SELECT ID, Type, FirstName, LastName, JudicialAssistant, Phone, Location, Street, City, Zip, County, Circuit, District, CourtRoom, HearingRoom, SubDivision FROM Judges WHERE Circuit = '{0}';", circuit);

                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collection.Add(new Judge()
                            {
                                ID = reader.GetValue("ID").ToString(),
                                Type = reader.GetValue("Type").ToString(),
                                FirstName = reader.GetValue("FirstName").ToString(),
                                LastName = reader.GetValue("LastName").ToString(),
                                JudicialAssistant = reader.GetValue("JudicialAssistant").ToString(),
                                Phone = reader.GetValue("Phone").ToString(),
                                Location = reader.GetValue("Location").ToString(),
                                Street = reader.GetValue("Street").ToString(),
                                City = reader.GetValue("City").ToString(),
                                Zip = reader.GetValue("Zip").ToString(),
                                County = reader.GetValue("County").ToString(),
                                Circuit = reader.GetValue("Circuit").ToString(),
                                District = reader.GetValue("District").ToString(),
                                CourtRoom = reader.GetValue("CourtRoom").ToString(),
                                HearingRoom = reader.GetValue("HearingRoom").ToString(),
                                SubDivision = reader.GetValue("SubDivision").ToString()
                            });
                        }
                    }
                }
            }

            return collection;
        }


        public static DataTable Export(string circuit)
        {
            DataTable table = new DataTable(circuit);

            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var sql = circuit.Equals("All") ? "SELECT ID, Type, FirstName, LastName, JudicialAssistant, Phone, Location, Street, City, Zip, County, Circuit, District, CourtRoom, HearingRoom, SubDivision FROM Judges;" : string.Format("SELECT ID, Type, FirstName, LastName, JudicialAssistant, Phone, Location, Street, City, Zip, County, Circuit, District, CourtRoom, HearingRoom, SubDivision FROM Judges WHERE Circuit = '{0}';", circuit);

                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                table.Columns.Add(new DataColumn(reader.GetName(i)));
                            }
                        }

                        int y = 0;

                        while (reader.Read())
                        {
                            DataRow row = table.NewRow();

                            for (int x = 0; x < reader.FieldCount; x++)
                            {
                                row[x] = reader.GetValue(x);
                            }

                            table.Rows.Add(row);
                        }
                    }
                }
            }

            return table;
        }
    }
}
