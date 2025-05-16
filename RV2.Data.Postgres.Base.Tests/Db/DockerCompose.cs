using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RV2.Data.Postgres.Base.Tests.Db
{
    public class DockerCompose : IDisposable
    {
        private readonly DockerClient _dockerClient;
        private string _containerId;
        public string ConnectionString { get; private set; }

        public DockerCompose()
        {
            var dockerUri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "npipe://./pipe/docker_engine"
                : "unix:///var/run/docker.sock";
            _dockerClient = new DockerClientConfiguration(new Uri(dockerUri))
                .CreateClient();
        }

        public async Task StartPostgresContainerAsync()
        {
            await CleanupExistingContainersAsync();

            var hostPort = GetAvailablePort();

            var container = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = "postgres:latest",
                Env = new List<string>
                {
                    "POSTGRES_PASSWORD=testpassword",
                    "POSTGRES_USER=testuser",
                    "POSTGRES_DB=testdb"
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            "5432/tcp",
                            new List<PortBinding> { new PortBinding { HostPort = hostPort.ToString() } }
                        }
                    },
                    AutoRemove = true 
                }
            });

            _containerId = container.ID;
            await _dockerClient.Containers.StartContainerAsync(_containerId, new ContainerStartParameters());

            ConnectionString = $"Host=localhost;Port={hostPort};Database=testdb;Username=testuser;Password=testpassword";

            await Task.Delay(10000);
        }

        private int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private async Task CleanupExistingContainersAsync()
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true });

            foreach (var container in containers.Where(c => c.Image == "postgres"))
            {
                try
                {
                    await _dockerClient.Containers.StopContainerAsync(container.ID,
                        new ContainerStopParameters());
                    await _dockerClient.Containers.RemoveContainerAsync(container.ID,
                        new ContainerRemoveParameters());
                }
                catch
                {
                    
                }
            }
        }

        public async Task DisposeAsync()
        {
            if (!string.IsNullOrEmpty(_containerId))
            {
                try
                {
                    await _dockerClient.Containers.StopContainerAsync(_containerId,
                        new ContainerStopParameters());
                }
                catch
                {
                    
                }
            }
            _dockerClient?.Dispose();
        }

        public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
    }
}
