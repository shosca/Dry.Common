#region using

using System;
using System.IO;
using Castle.ActiveRecord;
using Dry.Common;
using Dry.Common.ActiveRecord.Model;

#endregion

namespace Dry.Common.Model {
    public class FileInfo : BaseGuidModel<FileInfo> {
        public virtual string MimeType { get; set; }
        public virtual string FileName { get; set; }
        public virtual string Etag { get; set; }
        public virtual long Size { get; set; }
        public virtual void ReadFromFile(string file) {
            Data = new FileData(this, file) {Id = Id};
            Etag = Data.Data.ComputeMD5();
            Size = Data.Data.Length;
        }

        public virtual void ReadFromByteArray(byte[] data) {
            Data = new FileData(this, data) {Id = Id};
            Etag = Data.Data.ComputeMD5();
            Size = Data.Data.Length;
        }

        public virtual FileData Data { get; set; }

        public override void Save()
        {
            base.Save();
            if (Data != null) {
                Data.Id = Id;
                Data.Update();
            }
        }

        public override void SaveAndFlush()
        {
            base.SaveAndFlush();
            if (Data != null) {
                Data.Id = Id;
                Data.UpdateAndFlush();
            }
        }
    }

    public class FileData : BaseGuidModel<FileData> {
        public FileData() { }

        public FileData(FileInfo f, string file) {
            this.Id = f.Id;
            Data = File.ReadAllBytes(file);
            FileInfo = f;
        }

        public FileData(FileInfo f,byte[] data) {
            this.Id = f.Id;
            Data = data;
            FileInfo = f;
        }

        public virtual FileInfo FileInfo { get; set; }
        public virtual byte[] Data { get; set; }
    }
}
