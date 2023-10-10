using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Battlehub.Storage
{
    public struct TreeItem<TFID>
    {
        public TFID ParentID
        {
            get;
            private set;
        }

        public TFID ID
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public bool IsFolder
        {
            get;
            private set;
        }

        public TreeItem(TFID parentID, TFID id, string name, bool isFolder)
        {
            ParentID = parentID;
            ID = id;
            Name = name;
            IsFolder = isFolder;
        }
    }

    public interface IDataLayer<TFID>
    {
        Task<IList<TreeItem<TFID>>> GetTreeAsync(TFID rootID, bool recursive = true);
        Task<bool> ExistsAsync(TFID folderID);
        Task CreateFolderAsync(TFID folderID);
        Task MoveFolderAsync(TFID folderID, TFID newFolderID);
        Task DeleteFolderAsync(TFID folderID);
        Task<Stream> OpenReadAsync(TFID fileID);
        Task<Stream> OpenWriteAsync(TFID fileID);
        void ReleaseAsync(Stream stream);
        Task MoveAsync(TFID fileID, TFID newFileID);
        Task DeleteAsync(TFID fileID);
    }
}
