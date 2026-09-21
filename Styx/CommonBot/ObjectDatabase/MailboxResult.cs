using System;
using System.Data.SQLite;
using System.Globalization;

namespace Styx.CommonBot.ObjectDatabase
{
    public class MailboxResult : IEquatable<MailboxResult>
    {
        public int Entry { get; set; }
        public int MapId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public WoWPoint Location { get { return new WoWPoint(X, Y, Z); } }
        public uint FactionId { get; set; }

        internal MailboxResult(SQLiteDataReader reader)
        {
            Entry = reader.GetInt32(reader.GetOrdinal("entry"));
            FactionId = (uint)reader.GetInt32(reader.GetOrdinal("faction"));
            MapId = reader.GetInt32(reader.GetOrdinal("map"));
            X = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("x")).ToString().Replace(",", "."), CultureInfo.InvariantCulture);
            Y = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("y")).ToString().Replace(",", "."), CultureInfo.InvariantCulture);
            Z = Convert.ToSingle(reader.GetValue(reader.GetOrdinal("z")).ToString().Replace(",", "."), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.
        ///                 </param>
        public bool Equals(MailboxResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return FactionId == other.FactionId && MapId == other.MapId && Entry == other.Entry && Z.Equals(other.Z) && Y.Equals(other.Y) && X.Equals(other.X);
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
            if (obj.GetType() != GetType()) return false;
            return Equals((MailboxResult)obj);
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
                int result = (int)FactionId;
                result = (result * 397) ^ MapId;
                result = (result * 397) ^ Entry;
                result = (result * 397) ^ Z.GetHashCode();
                result = (result * 397) ^ Y.GetHashCode();
                result = (result * 397) ^ X.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(MailboxResult left, MailboxResult right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MailboxResult left, MailboxResult right)
        {
            return !Equals(left, right);
        }
    }
}
