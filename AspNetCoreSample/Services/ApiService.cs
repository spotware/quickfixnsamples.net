using System.Collections.Concurrent;

namespace AspNetCoreSample.Services
{
    public class ApiService
    {
        private readonly ConcurrentDictionary<string, FixClient> _clients = new();

        public void ConnectClient(ApiCredentials apiCredentials, string id)
        {
            var client = new FixClient(apiCredentials);

            client.Connect();

            _clients.AddOrUpdate(id, client, (id, oldClient) => client);
        }

        public FixClient GetClient(string id) => _clients[id];
    }
}