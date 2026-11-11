using System.IO.Pipes;

namespace ClaudeProfiles.Core.Instance;

public static class ManagerInstanceChannel
{
    private const int ConnectTimeoutMilliseconds = 400;

    private static string PipeName => $"ClaudeProfiles-{Environment.UserName}";

    public static bool TrySendToRunningInstance(ManagerMessage message)
    {
        using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);

        try
        {
            client.Connect(ConnectTimeoutMilliseconds);
        }
        catch (TimeoutException)
        {
            return false;
        }

        using var writer = new StreamWriter(client);
        writer.WriteLine(message.ToLine());
        return true;
    }

    public static async Task ListenAsync(Action<ManagerMessage> onMessage, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var server = new NamedPipeServerStream(
                PipeName, PipeDirection.In, maxNumberOfServerInstances: 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            await server.WaitForConnectionAsync(cancellationToken);

            using var reader = new StreamReader(server);
            string? line = await reader.ReadLineAsync(cancellationToken);

            if (line is not null && ManagerMessage.TryParseLine(line, out ManagerMessage? message))
            {
                onMessage(message);
            }
        }
    }
}
