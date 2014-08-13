using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsposePreviewGenerator
{
    public static class Configuration
    {
        private const string CHUNKSIZEKEY = "ChunkSize";
        private const int DEFAULCHUNKSIZE = 10485760;
        private static int? _chunkSizeInBytes;
        public static int ChunkSizeInBytes
        {
            get
            {
                if (!_chunkSizeInBytes.HasValue)
                {
                    int value;
                    if (!int.TryParse(ConfigurationManager.AppSettings[CHUNKSIZEKEY], out value))
                        value = DEFAULCHUNKSIZE;
                    _chunkSizeInBytes = value;
                }
                return _chunkSizeInBytes.Value;
            }
        }
    }
}
