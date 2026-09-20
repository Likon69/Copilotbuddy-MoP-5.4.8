using System;
using Styx.Patchables;

namespace Styx.WoWInternals
{
    public abstract class WoWLandMark
    {
        protected WoWLandMark(int index, LandMarkEntry data)
        {
            Index = index;
            landMarkEntry_0 = data;
        }

        public int Index { get; private set; }

        public LandMarkEntry Data
        {
            get { return landMarkEntry_0; }
        }

        public LandmarkType Type
        {
            get { return landMarkEntry_0.Type; }
        }

        public virtual bool IsValid
        {
            get { return true; }
        }

        public uint Entry
        {
            get { return landMarkEntry_0.Entry; }
        }

        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract int TextureIndex { get; }

        public abstract WoWPoint Location { get; }

        public abstract int NormalIcon { get; }

        public abstract int MapId { get; }

        public abstract int AreaId { get; }

        public abstract uint WorldState { get; }

        public abstract bool ShowInBattleMap { get; }

        public abstract bool ShowInBg { get; }

        public abstract bool ShowInZone { get; }

        public float MapX
        {
            get { return landMarkEntry_0.MapX; }
        }

        public float MapY
        {
            get { return landMarkEntry_0.MapY; }
        }

        public ArathiBasinLandmark ToArathiBasinLandmark()
        {
            return new ArathiBasinLandmark(Index, landMarkEntry_0);
        }

        public EyeOfTheStormLandmark ToEyeOfTheStormLandmark()
        {
            return new EyeOfTheStormLandmark(Index, landMarkEntry_0);
        }

        public BattleForGilneasLandmark ToBattleForGilneasLandmark()
        {
            return new BattleForGilneasLandmark(Index, landMarkEntry_0);
        }

        public AlteracValleyLandmark ToAlteracValleyLandmark()
        {
            return new AlteracValleyLandmark(Index, landMarkEntry_0);
        }

        public IsleOfConquestLandmark ToIsleOfConquestLandmark()
        {
            return new IsleOfConquestLandmark(Index, landMarkEntry_0);
        }

        public StrandOfTheAncientsLandmark ToStrandOfTheAncientsLandmark()
        {
            return new StrandOfTheAncientsLandmark(Index, landMarkEntry_0);
        }

        public DeepwindGorgeLandmark ToDeepwindGorgeLandmark()
        {
            return new DeepwindGorgeLandmark(Index, landMarkEntry_0);
        }

        public ResearchSiteLandmark ToResearchSiteLandmark()
        {
            return this as ResearchSiteLandmark;
        }

        public override string ToString()
        {
            return string.Format("[{0}: {1}]: Entry: {2} NormalIcon:{3} Location:{4} MapId:{5} AreaId:{6} WorldState:{7} ShowInBattleMap:{8} ShowInBg:{9} ShowInZone:{10} TextureIndex:{11} MapX:{12} MapY:{13}",
                Name, Description, Entry, NormalIcon, Location, MapId, AreaId, WorldState, ShowInBattleMap, ShowInBg, ShowInZone, TextureIndex, MapX, MapY);
        }

        private LandMarkEntry landMarkEntry_0;
    }
}
