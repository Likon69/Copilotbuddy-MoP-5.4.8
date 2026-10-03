using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Styx.Common;

namespace Styx
{
    internal class Spell
    {
        public string Name;
        public int Id;
    }

    internal class DbHelper
    {
        private Dictionary<int, Spell> _spellStrings;

        private static string FilePath = Path.Combine(Utilities.AssemblyDirectory, @"Spells.bin");

        public static DbHelper Instance { get; private set; }

        internal static void Initialize()
        {
            Instance = new DbHelper();
        }

        public DbHelper()
        {
            if (_spellStrings == null)
            {
                _spellStrings = new Dictionary<int, Spell>();
            }

            if (File.Exists(FilePath))
            {
                try
                {
                    ReadSpells(FilePath, out _spellStrings);
                }
                catch (Exception ex)
                {
                    Logging.WriteException(ex);
                }
            }
        }

        private static void ReadSpells(string file, out Dictionary<int, Spell> spells)
        {
            var ret = new Dictionary<int, Spell>();
            using (var br = new BinaryReader(File.OpenRead(file), Encoding.ASCII))
            {
                if (Encoding.ASCII.GetString(br.ReadBytes(4)) != "SPDB")
                {
                    throw new Exception("[DbHelper] File is not in the correct format");
                }

                if (br.ReadSingle() != 1.4f)
                {
                    throw new Exception("[DbHelper] Unknown file version");
                }

                int numRecs = br.ReadInt32();

                br.ReadInt32();

                for (int i = 0; i < numRecs; i++)
                {
                    var s = new Spell
                    {
                        Id = br.ReadInt32(),
                        Name = br.ReadString(),
                    };

                    ret.Add(s.Id, s);
                }
            }

            spells = ret;
        }

        private Spell GetSpell(int id)
        {
            Spell spell;
            if (_spellStrings.TryGetValue(id, out spell))
            {
                return spell;
            }

            return null;
        }

        public string GetName(int id)
        {
            var spell = GetSpell(id);
            if (spell != null)
            {
                return spell.Name;
            }

            return "";
        }
    }
}
