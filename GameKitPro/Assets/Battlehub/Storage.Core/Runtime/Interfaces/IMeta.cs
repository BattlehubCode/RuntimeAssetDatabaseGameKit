using System.Collections.Generic;

namespace Battlehub.Storage
{
    public interface IMeta<TID, TFID>
    {
        public TID ID { get; set; }

        public string Name { get; set; }

        //public Guid TypeID { get; set; }
        public int TypeID { get; set; }

        /// <summary>
        /// what I depend on
        /// </summary>
        public HashSet<TID> OutboundDependencies { get; set; }

        /// <summary>
        /// what depends on me
        /// </summary>
        public HashSet<TID> InboundDependencies { get; set; }

        public TFID ThumbnailFileID { get; set; }

        public TFID DataFileID { get; set; }

        public TFID FileID { get; set; }

        /// <summary>
        /// root instance id to asset id
        /// </summary>
        public Dictionary<TID, TID> Links { get; set; }

        /// <summary>
        /// asset id to instance id (for all objects in hierarchy)
        /// </summary>
        public Dictionary<TID, TID> GetLinkMap(TID instanceID);

        void AddLinkMap(TID instanceID, Dictionary<TID, TID> linkMap);

        void ClearLinkMaps();

        public string LoaderID { get; set; }

        public HashSet<TID> MarkedAsDestroyed { get; set; }

        bool HasLinks();
        
        bool HasOutboundDependencies();

        bool HasInboundDependencies();

        bool HasMarkAsDestroyed();
    }
}
