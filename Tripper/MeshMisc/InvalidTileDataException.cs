using System;

namespace Tripper.MeshMisc
{
    [Serializable]
    public class InvalidTileDataException : Exception
    {
        public InvalidTileDataException(string message)
            : base(message)
        {
        }
    }
}
