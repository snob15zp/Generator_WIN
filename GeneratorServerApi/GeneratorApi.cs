using GeneratorApiLibrary.Model;
using GeneratorServerApi;
using GeneratorServerApi.Model;
using RestSharp;
using RestSharp.Extensions;
using RestSharp.Serialization.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GeneratorApiLibrary
{

    public interface IGeneratorApi
    {
        Task<Firmware> GetLatestVersion();
        Task DonwloadFirmware(string version, string path);
    }

    public class GeneratorApi : IGeneratorApi
    {
        private RestClient client;

        public GeneratorApi(string baserUrl)
        {
            client = new RestClient(baserUrl);
            client.UseSerializer(
                () => new JsonSerializer()
            );
        }

        public Task DonwloadFirmware(string version, string path)
        {
            return Task.Run(() =>
            {
                var request = new RestRequest($"/api/firmware/{version}/download", Method.GET);
                var fileBytes = client.DownloadData(request);
                File.WriteAllBytes(path, fileBytes);
            });
        }

        public Task<Firmware> GetLatestVersion()
        {
            var request = new RestRequest("/api/firmware/latest", Method.GET);
            return Execute<Firmware>(request);
        }

        private async Task<T> Execute<T>(RestRequest request)
        {
            var response = await client.ExecuteAsync<T>(request);
            if (!response.IsSuccessful)
            {
                var error = new JsonDeserializer().Deserialize<ResponseError>(response);
                throw new ApiException(error.errors.status, error.errors.message);
            }
            return response.Data;
        }
    }
}
