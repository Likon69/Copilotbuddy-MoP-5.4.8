using System;
using System.Data;
using System.Data.SQLite;

namespace Bots.ArchaeologyBuddy.SQLiteHelper
{
    internal class SQLiteManager
    {
        private const string Password = "";
        private readonly String _dbConnection;
        /// <summary>
        ///     Default Constructor for SQLiteDatabase Class.
        /// </summary>
        public SQLiteManager(string filePath)
        {
            _dbConnection = String.Format("Data Source={0};Version=3;New=True;Compress=True;", filePath);
        }

        /// <summary>
        ///     Allows the programmer to run a query against the Database.
        /// </summary>
        /// <param name="sql">The SQL to run</param>
        /// <returns>A DataTable containing the result set.</returns>
        public DataTable GetDataTable(string sql)
        {
            var dt = new DataTable();
            try
            {
                var cnn = new SQLiteConnection(_dbConnection);
                cnn.SetPassword(Password);
                cnn.Open();
                var mycommand = new SQLiteCommand(cnn) { CommandText = sql };
                var reader = mycommand.ExecuteReader();
                dt.Load(reader);
                reader.Close();
                cnn.Close();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            return dt;
        }
    }
}