using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.Database
{
    internal static class NpcCacher
    {
        private static readonly WaitTimer Timer = WaitTimer.FiveSeconds;

        private static readonly SQLiteCommand ExistsCommand = new SQLiteCommand("SELECT id FROM npcs WHERE Entry = @entry AND MapId = @mapid AND VECTORDISTANCE(X,Y,Z,@XV,@YV,@ZV) < 1600;", Connection.Instance);

        private static readonly SQLiteCommand InsertCommand;

        private static readonly SQLiteCommand MailboxExistsCommand;

        private static readonly SQLiteCommand MailboxInsertCommand;

        private static readonly uint[] _excludedNpcEntries = new uint[] { 14337U, 24780U, 43929U, 33238U, 33239U, 35642U };

        private static readonly uint[] _excludedMailboxEntries = new uint[] { 191605U };

        private static readonly object _syncLock = new object();

        private static void RemoveBadVendors()
        {
            SQLiteCommand cmd = new SQLiteCommand("DELETE FROM npcs WHERE MapId IN (30, 529, 628, 607, 489)", Connection.Instance);
            Connection.ExecuteCommandWithParams(cmd);
        }

        static NpcCacher()
        {
            ExistsCommand.Parameters.Add(new SQLiteParameter("@entry", DbType.AnsiString));
            ExistsCommand.Parameters.Add(new SQLiteParameter("@mapid", DbType.AnsiString));
            ExistsCommand.Parameters.Add(new SQLiteParameter("@XV", DbType.AnsiString));
            ExistsCommand.Parameters.Add(new SQLiteParameter("@YV", DbType.AnsiString));
            ExistsCommand.Parameters.Add(new SQLiteParameter("@ZV", DbType.AnsiString));
            InsertCommand = new SQLiteCommand("INSERT INTO npcs (Entry, Name, NpcFlags, NpcFlags2, X, Y, Z, FactionId, Title, TrainerClass, MapId, Level) VALUES (@entry, @name, @flag, @flag2, @x, @y, @z, @faction, @title, @trainer_class, @map, @level)", Connection.Instance);
            InsertCommand.Parameters.Add(new SQLiteParameter("@entry", DbType.AnsiString));
            InsertCommand.Parameters.Add(new SQLiteParameter("@name", DbType.AnsiString));
            InsertCommand.Parameters.Add(new SQLiteParameter("@flag", DbType.AnsiString));
            InsertCommand.Parameters.Add(new SQLiteParameter("@flag2", DbType.AnsiString));
            InsertCommand.Parameters.Add(new SQLiteParameter("@x", DbType.AnsiString));
            InsertCommand.Parameters.Add(new SQLiteParameter("@y", DbType.AnsiString));
            InsertCommand.Parameters.Add(new SQLiteParameter("@z", DbType.AnsiString));
            InsertCommand.Parameters.Add(new SQLiteParameter("@faction", DbType.AnsiString));
            InsertCommand.Parameters.Add(new SQLiteParameter("@title", DbType.AnsiString));
            InsertCommand.Parameters.Add(new SQLiteParameter("@trainer_class", DbType.AnsiString));
            InsertCommand.Parameters.Add(new SQLiteParameter("@map", DbType.AnsiString));
            InsertCommand.Parameters.Add(new SQLiteParameter("@level", DbType.AnsiString));
            MailboxExistsCommand = Connection.CreateParameterizedCommand("SELECT id FROM mailbox WHERE entry = @entry AND map = @map AND VECTORDISTANCE(x,y,z,@x,@y,@z) < 50;");
            MailboxInsertCommand = Connection.CreateParameterizedCommand("INSERT INTO mailbox (entry, map, x, y, z, faction) VALUES (@entry, @map, @x, @y, @z, @faction);");
            RemoveBadVendors();
        }

        internal static void FixFactions()
        {
            WoWDb.DbTable dbTable = StyxWoW.Db[ClientDb.FactionTemplate];
            int minIndex = dbTable.MinIndex;
            int maxIndex = dbTable.MaxIndex;
            List<FactionTemplate> list = new List<FactionTemplate>();
            for (int i = minIndex; i < maxIndex; i++)
            {
                list.Add(FactionTemplate.FromId((uint)i));
            }
            int num = 0;
            foreach (FactionTemplate factionTemplate in list)
            {
                if (factionTemplate != null && factionTemplate.Faction != null)
                {
                    SQLiteCommand sqliteCommand = Connection.Instance.CreateCommand();
                    sqliteCommand.CommandText = string.Format("UPDATE npcs SET FactionId = {0} WHERE FactionId = {1} AND MapId IN (1, 0)", factionTemplate.Id, factionTemplate.Faction.Id);
                    num += sqliteCommand.ExecuteNonQuery();
                }
            }
            MessageBox.Show(num.ToString());
        }

        public static void Pulse()
        {
            try
            {
                if (Timer.IsFinished)
                {
                    var npcs = ObjectManager.GetObjectsOfType<WoWUnit>(false, false)
                        .Where(u => u.CanGossip && u.NpcFlags > 1U && u.OwnedByRoot == null && StyxWoW.WorldScene.WorldMap.GetMapAt(u.Location) != null && !_excludedNpcEntries.Contains(u.Entry) && !NpcAlreadyExists(u));

                    var npcList = npcs.Select(u => new NpcData
                    {
                        Entry = u.Entry,
                        FactionId = u.FactionId,
                        Level = u.Level,
                        X = u.Location.X,
                        Y = u.Location.Y,
                        Z = u.Location.Z,
                        MapId = StyxWoW.WorldScene.WorldMap.GetMapAt(u.Location).MapID,
                        Name = u.Name,
                        NpcFlags = u.NpcFlags,
                        NpcFlags2 = u.NpcFlags2,
                        Title = u.SubName,
                        TrainerClass = (int)GetClassFromTitle(u.SubName)
                    }).ToList();

                    if (npcList.Count != 0)
                    {
                        using (SQLiteTransaction sqliteTransaction = Connection.Instance.BeginTransaction())
                        {
                            foreach (NpcData woWNpc in npcList)
                            {
                                InsertCommand.Parameters["@entry"].Value = woWNpc.Entry;
                                InsertCommand.Parameters["@name"].Value = woWNpc.Name;
                                InsertCommand.Parameters["@flag"].Value = woWNpc.NpcFlags;
                                InsertCommand.Parameters["@flag2"].Value = woWNpc.NpcFlags2;
                                InsertCommand.Parameters["@x"].Value = woWNpc.X;
                                InsertCommand.Parameters["@y"].Value = woWNpc.Y;
                                InsertCommand.Parameters["@z"].Value = woWNpc.Z;
                                InsertCommand.Parameters["@faction"].Value = woWNpc.FactionId;
                                InsertCommand.Parameters["@title"].Value = woWNpc.Title;
                                InsertCommand.Parameters["@trainer_class"].Value = woWNpc.TrainerClass;
                                InsertCommand.Parameters["@map"].Value = woWNpc.MapId;
                                InsertCommand.Parameters["@level"].Value = woWNpc.Level;
                                Logging.WriteDiagnostic(Colors.Orange, string.Format("Inserting {0} [{1}] into the database!", woWNpc.Name, (UnitNPCFlags)woWNpc.NpcFlags));
                                if (InsertCommand.ExecuteNonQuery() != 1)
                                {
                                    Logging.WriteDiagnostic("Failed to insert " + woWNpc.Name);
                                }
                            }
                            sqliteTransaction.Commit();
                        }
                    }

                    var mailboxes = ObjectManager.GetObjectsOfType<WoWGameObject>(false, false)
                        .Where(go => go.SubType == WoWGameObjectType.Mailbox && !_excludedMailboxEntries.Contains(go.Entry))
                        .Select(go => new { gObj = go, map = StyxWoW.WorldScene.WorldMap.GetMapAt(go.Location) })
                        .Where(x => x.map != null && !MailboxAlreadyExists(x.gObj, x.map.MapID))
                        .Select(x => new MailboxData
                        {
                            Entry = x.gObj.Entry,
                            FactionId = x.gObj.FactionTemplateId,
                            X = x.gObj.Location.X,
                            Y = x.gObj.Location.Y,
                            Z = x.gObj.Location.Z,
                            MapId = x.map.MapID
                        }).ToList();

                    if (mailboxes.Any())
                    {
                        using (SQLiteTransaction sqliteTransaction2 = Connection.Instance.BeginTransaction())
                        {
                            foreach (MailboxData woWMailbox in mailboxes)
                            {
                                int num = Connection.ExecuteCommandWithParams(MailboxInsertCommand, new object[]
                                {
                                    woWMailbox.Entry,
                                    woWMailbox.MapId,
                                    woWMailbox.X,
                                    woWMailbox.Y,
                                    woWMailbox.Z,
                                    woWMailbox.FactionId
                                });
                                Logging.WriteDiagnostic(Colors.Orange, string.Format("Inserting mailbox on map {0} and at {1} into the database!", woWMailbox.MapId, new WoWPoint(woWMailbox.X, woWMailbox.Y, woWMailbox.Z)));
                                if (num != 1)
                                {
                                    Logging.WriteDiagnostic("Failed to insert mailbox on map {0} and at {1}", new object[] { woWMailbox.MapId, new WoWPoint(woWMailbox.X, woWMailbox.Y, woWMailbox.Z) });
                                }
                            }
                            sqliteTransaction2.Commit();
                        }
                    }

                    Timer.Reset();
                }
            }
            catch (Exception ex)
            {
                Logging.Write(ex.ToString());
            }
        }

        private static bool NpcAlreadyExists(WoWUnit unit)
        {
            ExistsCommand.Parameters["@entry"].Value = unit.Entry;
            ExistsCommand.Parameters["@mapid"].Value = StyxWoW.WorldScene.WorldMap.GetMapAt(unit.Location).MapID;
            ExistsCommand.Parameters["@XV"].Value = unit.Location.X;
            ExistsCommand.Parameters["@YV"].Value = unit.Location.Y;
            ExistsCommand.Parameters["@ZV"].Value = unit.Location.Z;
            using (SQLiteDataReader sqliteDataReader = ExistsCommand.ExecuteReader())
            {
                if (sqliteDataReader.HasRows)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool MailboxAlreadyExists(WoWGameObject gameObject, int mapId)
        {
            bool hasRows;
            using (SQLiteDataReader sqliteDataReader = Connection.ExecuteReaderWithParams(MailboxExistsCommand, new object[] { gameObject.Entry, mapId, gameObject.X, gameObject.Y, gameObject.Z }))
            {
                hasRows = sqliteDataReader.HasRows;
            }
            return hasRows;
        }

        public static WoWClass GetClassFromTitle(string title)
        {
            switch (title)
            {
            case "Druid Trainer":
            case "Druidenlehrer":
            case "Druidenlehrerin":
                return WoWClass.Druid;
            case "Rogue Trainer":
            case "Schurkenlehrer":
            case "Schurkenlehrerin":
                return WoWClass.Rogue;
            case "Warrior Trainer":
            case "Kriegerlehrer":
            case "Kriegerlehrerin":
                return WoWClass.Warrior;
            case "Paladin Trainer":
            case "Paladinlehrer":
            case "Paladinlehrerin":
                return WoWClass.Paladin;
            case "Hunter Trainer":
            case "Jägerlehrer":
            case "Jägerlehrerin":
                return WoWClass.Hunter;
            case "Priest Trainer":
            case "Priesterlehrer":
            case "Priesterlehrerin":
                return WoWClass.Priest;
            case "Death Knight Trainer":
                return WoWClass.DeathKnight;
            case "Shaman Trainer":
            case "Schamanenlehrer":
            case "Schamanenlehrerin":
                return WoWClass.Shaman;
            case "Mage Trainer":
            case "Magierlehrer":
            case "Magierlehrerin":
                return WoWClass.Mage;
            case "Warlock Trainer":
            case "Hexenmeisterlehrer":
            case "Hexenmeisterlehrerin":
                return WoWClass.Warlock;
            }
            return WoWClass.None;
        }

        private class NpcData
        {
            public uint Entry;
            public uint FactionId;
            public int Level;
            public float X;
            public float Y;
            public float Z;
            public int MapId;
            public string Name;
            public uint NpcFlags;
            public uint NpcFlags2;
            public string Title;
            public int TrainerClass;
        }

        private class MailboxData
        {
            public uint Entry;
            public uint FactionId;
            public float X;
            public float Y;
            public float Z;
            public int MapId;
        }
    }
}
