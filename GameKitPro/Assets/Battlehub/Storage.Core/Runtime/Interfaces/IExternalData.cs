using System.Collections.Generic;

namespace Battlehub.Storage
{
    public interface IExternalData<TID>
    {
        public string ExternalKey
        {
            get;
            set;
        }

        public Dictionary<string, TID> ExternalIDs
        {
            get;
            set;
        }
    }
}