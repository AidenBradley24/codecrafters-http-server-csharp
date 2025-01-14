using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public static class Server
{
    public static void Main(string[] args)
    {
        string fileStore = "";
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--directory")
            {
                fileStore = args[++i];
            }
        }

        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();

        while (true)
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
            Dictionary<string, string> headers = [];
            int lineIndex = 1;
            while (lineIndex < lines.Length && !string.IsNullOrEmpty(lines[lineIndex]))
            {
                string line = lines[lineIndex];
                string label = line[..line.IndexOf(':')];
                string detail = line[(line.IndexOf(':') + 2)..];
                headers.Add(label, detail);
                lineIndex++;
            }

            string? requestBody = method == "POST" ? string.Join("\r\n", lines[++lineIndex..])[..int.Parse(headers["Content-Length"])] : null;
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
                    FileInfo file = new(Path.Combine(fileStore, urlSections[2]));
                    switch (method)
                    {
                        case "GET":
                            {
                                if (file.Exists)
                                {
                                    content = File.ReadAllBytes(file.FullName);
                                }
                                else
                                {
                                    statusMessage = "404 Not Found";
                                }
                            }
                            break;
                        case "POST":
                            {
                                using var writeStream = file.OpenWrite();
                                writeStream.Write(Encoding.UTF8.GetBytes(requestBody!));
                                statusMessage = "201 Created";
                            }
                            break;
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
                headers.TryGetValue("Accept-Encoding", out string? encodings);
                HashSet<string> encodingOptions = encodings == null ? [] : [.. encodings?.Split(',').Select(s => s.Trim())];
                MemoryStream memoryStream = new();
                b.Append("Content-Type: text/plain\r\n");

                if (encodingOptions.Contains("gzip"))
                {
                    var gzip = new GZipStream(memoryStream, CompressionMode.Compress, true);
                    gzip.Write(Encoding.UTF8.GetBytes(sContent));
                    gzip.Flush();
                    gzip.Close();
                    b.Append($"Content-Encoding: gzip\r\n");
                }
                else
                {
                    b.Append($"Content-Length: {sContent.Length}\r\n");
                    memoryStream.Write(Encoding.UTF8.GetBytes(sContent));
                }

                finalContent = memoryStream.ToArray();
                b.Append($"Content-Length: {finalContent.Length}\r\n");
            }
            else if (content is byte[] bContent)
            {
                MemoryStream memoryStream = new(bContent);
                b.Append("Content-Type: application/octet-stream\r\n");
                b.Append($"Content-Length: {bContent.Length}\r\n");
                finalContent = memoryStream.ToArray();
            }

            b.Append("\r\n");

            stream.Write(Encoding.UTF8.GetBytes(b.ToString()));
            if (finalContent != null)
            {
                stream.Write(finalContent);
            }

            client.Dispose();
            return Task.CompletedTask;
        }
    }
}


