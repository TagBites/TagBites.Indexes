using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using Lucene.Net.Store;

namespace TagBites.Indexes
{
    internal class SearchIndexContainer : BaseDirectory
    {
        private ZipFile _zipFile;

        public SearchIndexContainer(Stream stream, bool ownStream)
            : this(new ICSharpCode.SharpZipLib.Zip.ZipFile(stream) { IsStreamOwner = ownStream })
        { }
        public SearchIndexContainer(ICSharpCode.SharpZipLib.Zip.ZipFile zipFile)
        {
            _zipFile = zipFile;
            m_lockFactory = new SingleInstanceLockFactory();
        }


        public override string[] ListAll() => _zipFile.Cast<ZipEntry>().Select(x => x.Name).ToArray();
        public override void Sync(ICollection<string> names) { }

        [Obsolete("this method will be removed in 5.0")]
        public override bool FileExists(string name) => _zipFile.GetEntry(name) != null;
        public override void DeleteFile(string name)
        {
            _zipFile.BeginUpdate();
            _zipFile.Delete(name);
            _zipFile.CommitUpdate();
        }
        public override long FileLength(string name)
        {
            var entry = _zipFile.GetEntry(name);
            return entry?.Size ?? 0;
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            var entry = _zipFile.GetEntry(name);
            if (entry != null)
                throw new InvalidOperationException();

            return new Output(this, name, new MemoryStream());
        }
        public override IndexInput OpenInput(string name, IOContext context)
        {
            var entry = _zipFile.GetEntry(name);
            if (entry == null)
                throw new FileNotFoundException();

            var stream = _zipFile.GetInputStream(entry);

            // ReSharper disable once PossibleNullReferenceException
            return new Input(name, stream, context);
        }

        protected void FlashOutput(string name, Stream stream)
        {
            _zipFile.BeginUpdate();
            stream.Seek(0, SeekOrigin.Begin);
            var ds = new DataSource(stream);
            _zipFile.Add(ds, name, CompressionMethod.Stored);
            _zipFile.CommitUpdate();
        }

        protected override void Dispose(bool disposing)
        {
            if (_zipFile != null)
            {
                _zipFile?.Close();
                _zipFile = null;
            }
        }

        private class DataSource : IStaticDataSource
        {
            private Stream Stream { get; }

            public DataSource(Stream stream)
            {
                Stream = stream;
            }


            public Stream GetSource()
            {
                return Stream;
            }
        }
        private class Output : BufferedIndexOutput
        {
            private readonly SearchIndexContainer _owner;
            private readonly string _name;
            private Stream _stream;

            public override long Length => _stream.Length;

            public Output(SearchIndexContainer owner, string name, Stream stream)
            {
                _owner = owner;
                _name = name;
                _stream = stream;
            }


            protected override void FlushBuffer(byte[] b, int offset, int size)
            {
                _stream.Write(b, offset, size);
            }
            [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
            public override void Seek(long pos)
            {
                base.Seek(pos);
                _stream.Seek(pos, SeekOrigin.Begin);
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposing)
                    return;

                base.Dispose(true);

                if (_stream != null)
                {
                    _stream.Flush();
                    _owner.FlashOutput(_name, _stream);
                    _stream = null;
                }
            }
        }
        private class Input : BufferedIndexInput
        {
            private Stream _stream;
            private readonly long _length;

            public sealed override long Length => _length;
            private bool IsClone { get; set; }

            public Input(string resourceDesc, Stream file, IOContext context)
                : base(resourceDesc, context)
            {
                _stream = file;
                _length = file.Length;
            }


            protected override void ReadInternal(byte[] b, int offset, int len)
            {
                lock (_stream)
                {
                    var offset1 = Position;
                    _stream.Seek(offset1, SeekOrigin.Begin);

                    if (offset1 + len > _length)
                        throw new EndOfStreamException("read past EOF: " + this);

                    try
                    {
                        _stream.Read(b, offset, len);
                    }
                    catch (IOException ex)
                    {
                        throw new IOException(ex.Message + ": " + this, ex);
                    }
                }
            }
            protected override void SeekInternal(long position) { }

            public override object Clone()
            {
                var clone = (Input)base.Clone();
                clone.IsClone = true;
                return clone;
            }
            protected override void Dispose(bool disposing)
            {
                if (!disposing || IsClone)
                    return;

                if (_stream != null)
                {
                    lock (_stream)
                    {
                        if (_stream != null)
                        {
                            _stream.Dispose();
                            _stream = null;
                        }
                    }
                }
            }
        }
    }
}
