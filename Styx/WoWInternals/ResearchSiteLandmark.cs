using System;
using Styx.Patchables;
using Styx.WoWInternals.DBC;

namespace Styx.WoWInternals
{
    public class ResearchSiteLandmark : WoWLandMark
    {
        public ResearchSiteLandmark(int index, LandMarkEntry data)
            : base(index, data)
        {
            researchSite_0 = ResearchSite.FromId(Entry);
        }

        public override bool IsValid
        {
            get { return base.IsValid && researchSite_0 != null; }
        }

        public override string Name
        {
            get { return researchSite_0.Name; }
        }

        public override string Description
        {
            get { return ""; }
        }

        public uint QuestPoiBlobId
        {
            get { return researchSite_0.QuestPoiBlobID; }
        }

        public override int TextureIndex
        {
            get { return researchSite_0.TextureIndex; }
        }

        public override WoWPoint Location
        {
            get { return WoWPoint.Zero; }
        }

        public override int NormalIcon
        {
            get { return 0; }
        }

        public override int MapId
        {
            get { return 0; }
        }

        public override int AreaId
        {
            get { return 0; }
        }

        public override uint WorldState
        {
            get { return 0U; }
        }

        public override bool ShowInBattleMap
        {
            get { return false; }
        }

        public override bool ShowInBg
        {
            get { return false; }
        }

        public override bool ShowInZone
        {
            get { return true; }
        }

        private readonly ResearchSite researchSite_0;
    }
}
