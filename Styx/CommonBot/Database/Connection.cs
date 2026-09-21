using System;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Text.RegularExpressions;
using Styx.Common;

namespace Styx.CommonBot.Database
{
    public static class Connection
    {
        private static SQLiteConnection _conn;
        private static Regex FindParamsRegex = new Regex("@[\\w_]*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static SQLiteConnection Instance
        {
            get
            {
                if (_conn == null)
                {
                    var csb = new SQLiteConnectionStringBuilder();
                    // Awesome password. I know. Love me!
                    csb.Password = "JkejXP5_fG2vN-jlFVME";
                    csb.DataSource = Utilities.AssemblyDirectory + "\\Data.bin";

                    csb.Pooling = true;

                    _conn = new SQLiteConnection(csb.ConnectionString);
                    _conn.StateChange += HandleConnectionStateChanged;
                    _conn.Open();

                    SQLiteFunction.RegisterFunction(typeof(DistanceFunction));
                    SQLiteFunction.RegisterFunction(typeof(PathDistanceFunction));
                }
                return _conn;
            }
        }

        private static void HandleConnectionStateChanged(object sender, StateChangeEventArgs e)
        {
            switch (e.CurrentState)
            {
                case ConnectionState.Closed:
                case ConnectionState.Broken:
                    // Try to reopen the connection if it closes, or breaks.
                    _conn.Close();
                    _conn.Open();
                    break;
                default:
                    // Do nothing...
                    break;
            }
        }

        internal static SQLiteCommand CreateParameterizedCommand(string commandText)
        {
            var ret = new SQLiteCommand(commandText, Instance);
            MatchCollection matches = FindParamsRegex.Matches(commandText);
            if (matches.Count != 0)
            {
                foreach (object m in matches)
                {
                    ret.Parameters.Add(new SQLiteParameter(m.ToString()));
                }
            }
            return ret;
        }

        /// <summary>
        /// Executes a command, and returns it's reader using the specified parameters.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="args">The parameters, in command order, that the command takes.</param>
        /// <returns></returns>
        internal static SQLiteDataReader ExecuteReaderWithParams(SQLiteCommand command, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                command.Parameters[i].Value = args[i];
            }

            return command.ExecuteReader();
        }

        internal static int ExecuteCommandWithParams(SQLiteCommand command, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                command.Parameters[i].Value = args[i];
            }

            return command.ExecuteNonQuery();
        }

        #region Nested type: DistanceFunction

        [SQLiteFunction(Arguments = 6, FuncType = FunctionType.Scalar, Name = "VECTORDISTANCE")]
        public sealed class DistanceFunction : SQLiteFunction
        {
            public override object Invoke(object[] args)
            {
                var pt1 = new WoWPoint(Convert.ToSingle(args[0], CultureInfo.InvariantCulture), Convert.ToSingle(args[1], CultureInfo.InvariantCulture), Convert.ToSingle(args[2], CultureInfo.InvariantCulture));
                var pt2 = new WoWPoint(Convert.ToSingle(args[3], CultureInfo.InvariantCulture), Convert.ToSingle(args[4], CultureInfo.InvariantCulture), Convert.ToSingle(args[5], CultureInfo.InvariantCulture));

                return pt1.DistanceSqr(pt2);
            }
        }
        [SQLiteFunction(Arguments = 6, FuncType = FunctionType.Scalar, Name = "PATHDISTANCE")]
        public sealed class PathDistanceFunction : SQLiteFunction
        {
            public override object Invoke(object[] args)
            {
                var pt1 = new WoWPoint(Convert.ToSingle(args[0], CultureInfo.InvariantCulture), Convert.ToSingle(args[1], CultureInfo.InvariantCulture), Convert.ToSingle(args[2], CultureInfo.InvariantCulture));
                var pt2 = new WoWPoint(Convert.ToSingle(args[3], CultureInfo.InvariantCulture), Convert.ToSingle(args[4], CultureInfo.InvariantCulture), Convert.ToSingle(args[5], CultureInfo.InvariantCulture));

                return pt1.DistanceSqr(pt2);
            }
        }

        #endregion
    }
}
