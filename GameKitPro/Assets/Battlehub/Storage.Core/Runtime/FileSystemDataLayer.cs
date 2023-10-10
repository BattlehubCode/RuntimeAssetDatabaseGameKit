using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Battlehub.Storage
{
    public class FileSystemDataLayer : IDataLayer<string>
    {
        public Task<IList<TreeItem<string>>> GetTreeAsync(string rootID, bool recursive = true)
        {
            string[] folders = Directory.GetDirectories(rootID, "*", SearchOption.AllDirectories);
            string[] files = Directory.GetFiles(rootID, "*.meta", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            IList<TreeItem<string>> result = new List<TreeItem<string>>(1 + folders.Length + files.Length)
            {
                CreateTreeItem(rootID, isFolder: true)
            };

            for (int i = 0; i < folders.Length; ++i)
            {
                var folder = folders[i];
                result.Add(CreateTreeItem(folder, isFolder: true));
            }

            for (int i = 0; i < files.Length; ++i)
            {
                var file = files[i];
                result.Add(CreateTreeItem(file, isFolder: false));
            }

            return Task.FromResult(result);
        }

        public Task<bool> ExistsAsync(string id)
        {
            return Task.FromResult(File.Exists(id) || Directory.Exists(id));
        }

        public Task CreateFolderAsync(string id)
        {
            Directory.CreateDirectory(id);
            return Task.CompletedTask;
        }

        public Task DeleteFolderAsync(string id)
        {
            if (Directory.Exists(id))
            {
                Directory.Delete(id, true);
            }
            return Task.CompletedTask;
        }

        public Task MoveFolderAsync(string folderID, string newFolderID)
        {
            Directory.Move(folderID, newFolderID);
            return Task.CompletedTask;
        }

        private string NormalizePath(string path)
        {
            return path.Replace("\\", "/");
        }

        private TreeItem<string> CreateTreeItem(string path, bool isFolder)
        {
            return new TreeItem<string>(
                NormalizePath(Path.GetDirectoryName(path)),
                NormalizePath(path),
                Path.GetFileNameWithoutExtension(path),
                isFolder: isFolder);
        }

        public Task<Stream> OpenReadAsync(string fileID)
        {
            Stream stream = File.OpenRead(fileID);
            return Task.FromResult(stream);
        }

        public Task<Stream> OpenWriteAsync(string fileID)
        {
            Stream stream = File.Open(fileID, FileMode.Create);
            return Task.FromResult(stream);
        }

        public void ReleaseAsync(Stream stream)
        {
            stream.Close();
        }

        public Task DeleteAsync(string id)
        {
            if(File.Exists(id))
            {
                File.Delete(id);
            }

            if(Directory.Exists(id))
            {
                Directory.Delete(id);
            }

            return Task.CompletedTask;
        }

        public Task MoveAsync(string fileID, string newFileID)
        {
            File.Move(fileID, newFileID);
            return Task.CompletedTask;
        }
    }
}
