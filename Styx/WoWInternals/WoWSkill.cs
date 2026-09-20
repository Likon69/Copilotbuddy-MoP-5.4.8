using System;
using System.Runtime.InteropServices;
using Styx.WoWInternals.DBC;

namespace Styx.WoWInternals
{
    public class WoWSkill
    {
        internal WoWSkill(ushort id, ushort step, ushort value, ushort maxValue, short modifier, ushort bonus)
        {
            _skillInfo = default(SkillInfoStruct);
            _skillInfo.Id = id;
            _skillInfo.MaxValue = maxValue;
            _skillInfo.SkillStep = step;
            _skillInfo.Modifier = modifier;
            _skillInfo.Value = value;
            _skillInfo.Bonus = bonus;
            _skillLineInfo = SkillLineInfo.FromId((uint)id);
        }

        internal WoWSkill(IntPtr ptr)
        {
            _ptr = ptr;
            if (_ptr != (IntPtr)0)
            {
                _skillInfo = StyxWoW.Memory.Read<SkillInfoStruct>(ptr);
                _skillLineInfo = SkillLineInfo.FromId((uint)_skillInfo.Id);
            }
        }

        public bool IsValid
        {
            get { return _skillInfo.Id != 0 && _skillLineInfo != null; }
        }

        public int Id
        {
            get { return _skillInfo.Id; }
        }

        public SkillLineCategory CategoryId
        {
            get { return _skillLineInfo.Category; }
        }

        public string Name
        {
            get { return _skillLineInfo.Name; }
        }

        public int SpellIcon
        {
            get { return _skillLineInfo.SpellIconId; }
        }

        public bool CanLink
        {
            get { return _skillLineInfo.CanLink; }
        }

        public int MaxValue
        {
            get { return _skillInfo.MaxValue; }
        }

        public uint Bonus
        {
            get { return _skillInfo.Bonus; }
        }

        public int CurrentValue
        {
            get { return _skillInfo.Value; }
        }

        public override string ToString()
        {
            return string.Format("[{0}] CurrentValue:{1} MaxValue:{2}", Name, CurrentValue, MaxValue);
        }

        private readonly IntPtr _ptr;

        private readonly SkillLineInfo _skillLineInfo;

        private SkillInfoStruct _skillInfo;

        [StructLayout(LayoutKind.Sequential)]
        private struct SkillInfoStruct
        {
            public ushort Id;

            public ushort SkillStep;

            public ushort Value;

            public ushort MaxValue;

            public short Modifier;

            public ushort Bonus;
        }
    }
}
