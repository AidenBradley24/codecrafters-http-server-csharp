using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.IO;

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while(true)
{
    var client = server.AcceptTcpClient();
    Task.Run(async () => { await HandleClient(client); });
}

Task HandleClient(TcpClient client)
{
    using NetworkStream stream = client.GetStream();
    byte[] buffer = new byte[1024];
    stream.Read(buffer);

    string request = Encoding.UTF8.GetString(buffer);
    string[] lines = request.Split("\r\n");

    string[] requestLine = lines[0].Split(' ');
    string method = requestLine[0];
    string url = requestLine[1];
    string httpVersion = requestLine[2];

    // headers
    Dictionary<string, string> headers = new();
    int lineIndex = 1;
    while (!string.IsNullOrEmpty(lines[lineIndex]))
    {
        string line = lines[lineIndex];
        string label = line[..line.IndexOf(':')];
        string detail = line[(line.IndexOf(':') + 2)..];
        headers.Add(label, detail);
        lineIndex++;
    }

    string[] urlSections = url.Split('/');
    string statusMessage = "200 OK";
    object? content = null;
    switch (urlSections[1])
    {
        case "":
            // do nothing
            break;
        case "echo":
            content = urlSections[2];
            break;
        case "user-agent":
            content = headers["User-Agent"];
            break;
        case "files":
            FileInfo file = new(urlSections[2]);
            Console.WriteLine(file.FullName);
            if (file.Exists)
            {
                content = File.ReadAllBytes(file.FullName);
            }
            else
            {
                statusMessage = "404 Not Found";
            }
            break;
        default:
            statusMessage = "404 Not Found";
            break;
    }

    StringBuilder b = new();
    b.Append($"{httpVersion} {statusMessage}\r\n");

    byte[]? finalContent = null;
    if (content is string sContent)
    {
        b.Append("Content-Type: text/plain\r\n");
        b.Append($"Content-Length: {sContent.Length}\r\n");
        finalContent = Encoding.UTF8.GetBytes(sContent);
    }
    else if (content is byte[] bContent)
    {
        b.Append("Content-Type: application/octet-stream\r\n");
        b.Append($"Content-Length: {bContent.Length}\r\n");
        finalContent = bContent;
    }

    b.Append("\r\n");
    stream.Write(Encoding.UTF8.GetBytes(b.ToString()));
    if (finalContent != null) stream.Write(finalContent);

    client.Dispose();
    return Task.CompletedTask;
}
