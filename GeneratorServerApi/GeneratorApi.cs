using GeneratorApiLibrary.Model;
using GeneratorServerApi;
using GeneratorServerApi.Model;
using RestSharp;
using RestSharp.Serialization.Json;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GeneratorApiLibrary
{

    public interface IGeneratorApi
    {
        Task<Firmware> GetLatestVersion();
        Task DonwloadFirmware(string version, string path, CancellationToken cancellationToken);
        Task DownloadPrograms(string url, string path, CancellationToken cancellationToken);
        Task<Folder> GetFolder(string folderId, string token, CancellationToken cancellationToken);
    }

    public class GeneratorApi : IGeneratorApi
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(GeneratorApi));

        private RestClient client;

        public GeneratorApi(string baserUrl)
        {
            client = new RestClient(baserUrl);
            client.UseSerializer(
                () => new JsonSerializer()
            );
        }

        public Task<Folder> GetFolder(string folderId, string token, CancellationToken cancellationToken)
        {
            var requset = new RestRequest($"/api/folders/{folderId}/h/{token}");
            return Execute<Folder>(requset, cancellationToken);
        }

        public Task DonwloadFirmware(string version, string path, CancellationToken cancellationToken)
        {
            return DownloadFile(path, $"/firmware/{version}/download", cancellationToken);
        }

        public Task DownloadPrograms(string url, string path, CancellationToken cancellationToken)
        {
            return DownloadFile(path, url, cancellationToken);
        }

        public Task<Firmware> GetLatestVersion()
        {
            var request = new RestRequest("/api/firmware/latest", Method.GET);
            return Execute<Firmware>(request);
        }

        private async Task DownloadFile(string path, string url, CancellationToken cancellationToken)
        {
            var request = new RestRequest($"/api{url}", Method.GET);
            var result = await ExecuteRaw(request, cancellationToken);
            if (result != null)
            {
                File.WriteAllBytes(path, result);
            }

        }

        private async Task<T> Execute<T>(RestRequest request, CancellationToken cancellationToken = default)
        {
            var response = await client.ExecuteAsync<T>(request, cancellationToken);
            HandleResponseError(response);
            return response.Data;
        }

        private async Task<byte[]> ExecuteRaw(RestRequest request, CancellationToken cancellationToken = default)
        {
            var response = await client.ExecuteAsync(request, cancellationToken);
            HandleResponseError(response);
            return response.RawBytes;
        }

        private void HandleResponseError(IRestResponse response)
        {
            if (!response.IsSuccessful)
            {
                Logger.Error($"Api error: {response.StatusCode}, {response.ErrorMessage}, {response.Content}", response.ErrorException);
                var error = new JsonDeserializer().Deserialize<ResponseError>(response);
                throw new ApiException(error.Errors.Status, error.Errors.Message);
            }
        }

        internal class CancelableFileStream : FileStream
        {
            private CancellationToken token;
            public CancelableFileStream(string path, CancellationToken cancellationToken)
             : base(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)
            {
                this.token = cancellationToken;
            }

            public override void Write(byte[] array, int offset, int count)
            {
                base.Write(array, offset, count);
                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }
            }
        }
    }
}

