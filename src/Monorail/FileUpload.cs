namespace Dry.Common.Monorail {
    public class FileUpload {
        public string Name { get; private set; }
        public string Path { get; private set; }
        public string Type { get; private set; }
        public long Size { get; private set; }
        public int Order { get; set; }
        public string Error { get; set; }

        public FileUpload(string name, string path, string type, long size) {
            Name = name;
            Path = path;
            Type = type;
            Size = size;
        }
    }
}