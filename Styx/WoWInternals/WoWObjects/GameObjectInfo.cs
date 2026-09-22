using System;
using System.Collections.Generic;
using Styx.WoWInternals.WoWCache;

namespace Styx.WoWInternals.WoWObjects
{
    public class GameObjectInfo
    {
        private readonly WoWCache.WoWCache.InfoBlock _infoBlock;
        private KeyValuePair<DateTime, WoWCache.WoWCache.GameObjectCacheEntry>? _cachedGameObjectInfo;
        private List<ItemInfo> _cachedQuestItems;

        private GameObjectInfo(WoWCache.WoWCache.InfoBlock block)
        {
            _infoBlock = block;
        }

        /// <summary>
        /// For debugging purposes only.
        /// </summary>
        public IntPtr BaseAddress
        {
            get { return _infoBlock != null ? _infoBlock.Address : IntPtr.Zero; }
        }

        public uint Id
        {
            get { return _infoBlock.Id; }
        }

        /// <summary>
        /// Returns true when this object is available in WoW's cache.
        /// </summary>
        public bool IsCached
        {
            get { return _infoBlock != null; }
        }

        public List<ItemInfo> QuestItems
        {
            get { return GetQuestItems(); }
        }

        private WoWCache.WoWCache.GameObjectCacheEntry _inf
        {
            get { return InternalInfo; }
        }

        public WoWCache.WoWCache.GameObjectCacheEntry InternalInfo
        {
            get { return GetGameObjectInfo(); }
        }

        public WoWCache.WoWCache.GameObjectCacheEntry GetGameObjectInfo()
        {
            DateTime now = DateTime.Now;
            if (!_cachedGameObjectInfo.HasValue ||
                now.Subtract(_cachedGameObjectInfo.Value.Key).TotalMilliseconds > 3000.0)
            {
                _cachedGameObjectInfo =
                    new KeyValuePair<DateTime, WoWCache.WoWCache.GameObjectCacheEntry>(now, _infoBlock.GameObject);
            }

            return _cachedGameObjectInfo.Value.Value;
        }

        public List<ItemInfo> GetQuestItems()
        {
            if (_cachedQuestItems == null)
            {
                _cachedQuestItems = new List<ItemInfo>();
                uint[] questItems = _inf.QuestItems;
                for (int i = 0; i < questItems.Length; i++)
                {
                    if (questItems[i] <= 0U)
                        continue;

                    ItemInfo itemInfo = ItemInfo.FromId(questItems[i]);
                    if (itemInfo != null)
                        _cachedQuestItems.Add(itemInfo);
                }
            }

            return _cachedQuestItems;
        }

        /// <summary>
        /// Constructs an instance from a game object ID, or returns null when no cached information is available.
        /// </summary>
        public GameObjectInfo FromId(uint gameObjectId)
        {
            WoWCache.WoWCache.InfoBlock block = StyxWoW.Cache[CacheDb.GameObject].GetInfoBlockById(gameObjectId);
            return block != null ? new GameObjectInfo(block) : null;
        }
    }
}
