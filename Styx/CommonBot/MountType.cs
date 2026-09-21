using System;

namespace Styx.CommonBot
{
    public enum MountType
    {
        Unknown = -1,
        EpicGroundOnly = 225,
        Flying = 229,
        Ground = 230,
        Underwater = 231,
        UnderwaterVashjir = 232,
        TransformFlight = 238,
        AhnQiraj = 241,
        PandaFlyingMount = 247,
        ProfessionMount = -1,
        /// <summary> This is both flying and ground mounts, headless horseman has this type. </summary>
        Scaling = 248,
    }
}
