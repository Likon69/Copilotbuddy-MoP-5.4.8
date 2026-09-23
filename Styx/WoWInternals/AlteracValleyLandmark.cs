using System;
using Styx.Patchables;

namespace Styx.WoWInternals
{
    public class AlteracValleyLandmark : AreaPoiLandmark
    {
        public AlteracValleyLandmark(int index, LandMarkEntry data)
            : base(index, data)
        {
        }

        public AlteracValleyLandmarkType LandmarkType
        {
            get { return GetMarkType(this); }
        }

        public LandmarkControlType ControlType
        {
            get
            {
                switch (NormalIcon)
                {
                    case 1:
                    case 7:
                    case 8:
                        return LandmarkControlType.Uncontrolled;
                    case 2:
                    case 10:
                    case 13:
                        return LandmarkControlType.HordeControlled;
                    case 3:
                    case 11:
                    case 15:
                        return LandmarkControlType.AllianceControlled;
                    case 4:
                    case 9:
                    case 12:
                    case 14:
                        return LandmarkControlType.InConflict;
                    default:
                        return LandmarkControlType.Unknown;
                    case 6:
                        return LandmarkControlType.Destroyed;
                }
            }
        }

        private static AlteracValleyLandmarkType GetMarkType(WoWLandMark landmark)
        {
            uint entry = landmark.Entry;
            uint num = entry;
            if (num <= 1255U)
            {
                switch (num)
                {
                    case 1099U:
                        return AlteracValleyLandmarkType.IrondeepMine;
                    case 1100U:
                        return AlteracValleyLandmarkType.StonehearthOutpost;
                    case 1101U:
                        return AlteracValleyLandmarkType.IcebloodGarrison;
                    case 1102U:
                        return AlteracValleyLandmarkType.ColdtoothMine;
                    case 1103U:
                        return AlteracValleyLandmarkType.FrostwolfKeep;
                    default:
                        switch (num)
                        {
                            case 1208U:
                                return AlteracValleyLandmarkType.StormpikeGraveyard;
                            case 1209U:
                                return AlteracValleyLandmarkType.SnowfallGraveyard;
                            case 1210U:
                                return AlteracValleyLandmarkType.FrostwolfGraveyard;
                            default:
                                switch (num)
                                {
                                    case 1249U:
                                        return AlteracValleyLandmarkType.DunBaldarSouthBunker;
                                    case 1250U:
                                        break;
                                    case 1251U:
                                        return AlteracValleyLandmarkType.IcewingBunker;
                                    case 1252U:
                                        return AlteracValleyLandmarkType.IcebloodTower;
                                    case 1253U:
                                        return AlteracValleyLandmarkType.Unknown;
                                    case 1254U:
                                        return AlteracValleyLandmarkType.TowerPoint;
                                    case 1255U:
                                        return AlteracValleyLandmarkType.EastFrostwolfTower;
                                    default:
                                        return AlteracValleyLandmarkType.Unknown;
                                }
                                break;
                        }
                        break;
                }
            }
            else if (num <= 1530U)
            {
                switch (num)
                {
                    case 1328U:
                        return AlteracValleyLandmarkType.DunBaldar;
                    case 1329U:
                    case 1330U:
                    case 1331U:
                    case 1332U:
                    case 1333U:
                    case 1334U:
                    case 1335U:
                    case 1336U:
                    case 1337U:
                    case 1338U:
                    case 1339U:
                    case 1340U:
                    case 1341U:
                    case 1342U:
                    case 1343U:
                    case 1344U:
                    case 1345U:
                    case 1346U:
                    case 1360U:
                    case 1361U:
                    case 1365U:
                    case 1366U:
                    case 1367U:
                    case 1385U:
                    case 1401U:
                    case 1402U:
                    case 1403U:
                    case 1404U:
                        return AlteracValleyLandmarkType.Unknown;
                    case 1347U:
                    case 1389U:
                    case 1390U:
                    case 1391U:
                        return AlteracValleyLandmarkType.StonehearthBunker;
                    case 1348U:
                    case 1395U:
                    case 1396U:
                    case 1397U:
                        return AlteracValleyLandmarkType.StormpikeAidStation;
                    case 1349U:
                    case 1374U:
                    case 1375U:
                    case 1376U:
                        return AlteracValleyLandmarkType.IcebloodGraveyard;
                    case 1350U:
                    case 1392U:
                    case 1393U:
                    case 1394U:
                        return AlteracValleyLandmarkType.StonehearthGraveyard;
                    case 1351U:
                    case 1371U:
                    case 1372U:
                    case 1373U:
                        return AlteracValleyLandmarkType.FrostwolfReliefHut;
                    case 1352U:
                    case 1353U:
                    case 1354U:
                        break;
                    case 1355U:
                    case 1356U:
                    case 1357U:
                        return AlteracValleyLandmarkType.DunBaldarSouthBunker;
                    case 1358U:
                    case 1359U:
                        return AlteracValleyLandmarkType.ColdtoothMine;
                    case 1362U:
                    case 1363U:
                    case 1364U:
                        return AlteracValleyLandmarkType.EastFrostwolfTower;
                    case 1368U:
                    case 1369U:
                    case 1370U:
                        return AlteracValleyLandmarkType.FrostwolfGraveyard;
                    case 1377U:
                    case 1378U:
                    case 1379U:
                        return AlteracValleyLandmarkType.IcebloodTower;
                    case 1380U:
                    case 1381U:
                    case 1382U:
                        return AlteracValleyLandmarkType.IcewingBunker;
                    case 1383U:
                    case 1384U:
                        return AlteracValleyLandmarkType.IrondeepMine;
                    case 1386U:
                    case 1387U:
                    case 1388U:
                        return AlteracValleyLandmarkType.SnowfallGraveyard;
                    case 1398U:
                    case 1399U:
                    case 1400U:
                        return AlteracValleyLandmarkType.StormpikeGraveyard;
                    case 1405U:
                    case 1406U:
                    case 1407U:
                        return AlteracValleyLandmarkType.TowerPoint;
                    default:
                        switch (num)
                        {
                            case 1527U:
                            case 1528U:
                            case 1529U:
                            case 1530U:
                                return AlteracValleyLandmarkType.WestFrostwolfTower;
                            default:
                                return AlteracValleyLandmarkType.Unknown;
                        }
                        break;
                }
            }
            else
            {
                switch (num)
                {
                    case 1568U:
                        return AlteracValleyLandmarkType.WildpawCavern;
                    case 1569U:
                        return AlteracValleyLandmarkType.IcewingCavern;
                    default:
                        if (num != 1682U)
                        {
                            return AlteracValleyLandmarkType.Unknown;
                        }
                        return AlteracValleyLandmarkType.SnowfallGraveyard;
                }
            }

            return AlteracValleyLandmarkType.DunBaldarNorthBunker;
        }
    }
}
