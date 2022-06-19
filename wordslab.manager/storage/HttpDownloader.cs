using System.IO.Compression;

namespace wordslab.manager.storage
{
    // https://stackoverflow.com/questions/20661652/progress-bar-with-httpclient
    public class HttpDownloader : IDisposable
    {
        private readonly string _downloadUrl;
        private readonly string _destinationFilePath;
        private readonly bool _gunzip;

        private HttpClient _httpClient;

        public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

        public event ProgressChangedHandler ProgressChanged;

        public HttpDownloader(string downloadUrl, string destinationFilePath, bool gunzip = false)
        {
            _downloadUrl = downloadUrl;
            _destinationFilePath = destinationFilePath;
            _gunzip = gunzip;
        }

        public async Task StartDownload()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1) };

            using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                await DownloadFileFromHttpResponseMessage(response);
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            using (var contentStream = await response.Content.ReadAsStreamAsync())
                await ProcessContentStream(totalBytes, contentStream);
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream downloadStream)
        {
            Stream contentStream = downloadStream;

            InternalStreamObserver internalStreamObserver = null;
            if (_gunzip)
            {
                internalStreamObserver = new InternalStreamObserver(downloadStream);
                contentStream = new GZipStream(internalStreamObserver, CompressionMode.Decompress);
            }

            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            using (var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                do
                {
                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    if (_gunzip)
                    {
                        totalBytesRead += internalStreamObserver.BytesReadSinceLastCall;
                    }
                    else
                    {
                        totalBytesRead += bytesRead;
                    }
                    readCount += 1;

                    if (readCount % 100 == 0)
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                }
                while (isMoreToRead);
            }
        }

        private class InternalStreamObserver : Stream
        {
            private Stream internalStream;
            private long bytesRead;
            private long bytesWrite;
            private long bytesSeek;

            public InternalStreamObserver(Stream internalStream)
            {
                this.internalStream = internalStream;
            }

            public long BytesReadSinceLastCall { get { var result = bytesRead; bytesRead = 0; return result; } }
            public long BytesWriteSinceLastCall { get { var result = bytesWrite; bytesWrite = 0; return result; } }
            public long BytesSeekSinceLastCall { get { var result = bytesSeek; bytesSeek = 0; return result; } }

            public override bool CanRead => internalStream.CanRead;

            public override bool CanSeek => internalStream.CanSeek;

            public override bool CanWrite => internalStream.CanWrite;

            public override long Length => internalStream.Length;

            public override long Position { get => internalStream.Position; set => internalStream.Position = value; }

            public override void Flush()
            {
                internalStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var bytes = internalStream.Read(buffer, offset, count);
                bytesRead += bytes;
                return bytes;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                var bytes = internalStream.Seek(offset, origin);
                bytesSeek += bytes;
                return bytes;
            }

            public override void SetLength(long value)
            {
                internalStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                internalStream.Write(buffer, offset, count);
                bytesWrite += count;
            }
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {
            if (ProgressChanged == null)
                return;

            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);

            ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
