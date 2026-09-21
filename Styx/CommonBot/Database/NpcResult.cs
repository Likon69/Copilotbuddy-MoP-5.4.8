using System;
using System.Data.SQLite;
using System.Globalization;

namespace Styx.CommonBot.Database
{
    public class NpcResult : IEquatable<NpcResult>
    {
        public int Entry { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public int MapId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public WoWPoint Location { get { return new WoWPoint(X, Y, Z); } }
        public uint NpcFlags { get; set; }
        public uint NpcFlags2 { get; set; }
        public int Level { get; set; }
        public uint Faction { get; set; }
        public int TrainerClass { get; set; }

        internal NpcResult(SQLiteDataReader reader)
        {
            Entry = reader.GetInt32(reader.GetOrdinal("Entry"));
            Name = reader.GetString(reader.GetOrdinal("Name"));
            if (!reader.IsDBNull(reader.GetOrdinal("Title")))
                Title = reader.GetString(reader.GetOrdinal("Title"));

            X = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("X")).ToString().Replace(",", "."), CultureInfo.InvariantCulture);
            Y = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("Y")).ToString().Replace(",", "."), CultureInfo.InvariantCulture);
            Z = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("Z")).ToString().Replace(",", "."), CultureInfo.InvariantCulture);
            NpcFlags = (uint)reader.GetInt32(reader.GetOrdinal("NpcFlags"));
            NpcFlags2 = (uint)reader.GetInt32(reader.GetOrdinal("NpcFlags2"));
            Faction = (uint)reader.GetInt32(reader.GetOrdinal("FactionId"));
            MapId = reader.GetInt32(reader.GetOrdinal("MapId"));
            Level = reader.GetInt32(reader.GetOrdinal("Level"));
            TrainerClass = reader.GetInt32(reader.GetOrdinal("TrainerClass"));
        }

        public static bool operator ==(NpcResult left, NpcResult right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(NpcResult left, NpcResult right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.
        ///                 </param>
        public bool Equals(NpcResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.X.Equals(X) && other.Y.Equals(Y) && other.Z.Equals(Z) && other.Entry == Entry && Equals(other.Name, Name) && Equals(other.Title, Title) && other.MapId == MapId && other.NpcFlags == NpcFlags && other.Faction == Faction && other.NpcFlags2 == NpcFlags2 && other.TrainerClass == TrainerClass && other.Level == Level;
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.
        ///                 </param><exception cref="T:System.NullReferenceException">The <paramref name="obj"/> parameter is null.
        ///                 </exception><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(NpcResult)) return false;
            return Equals((NpcResult)obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = X.GetHashCode();
                result = (result * 397) ^ Y.GetHashCode();
                result = (result * 397) ^ Z.GetHashCode();
                result = (result * 397) ^ Entry;
                result = (result * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                result = (result * 397) ^ (Title != null ? Title.GetHashCode() : 0);
                result = (result * 397) ^ MapId;
                result = (result * 397) ^ NpcFlags.GetHashCode();
                result = (result * 397) ^ Faction.GetHashCode();
                result = (result * 397) ^ NpcFlags2.GetHashCode();
                result = (result * 397) ^ Level;
                result = (result * 397) ^ TrainerClass;
                return result;
            }
        }
    }
}
