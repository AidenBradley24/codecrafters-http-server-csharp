using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
using var socket = server.AcceptSocket();

byte[] buffer = new byte[1024];
socket.Receive(buffer);
string request = Encoding.UTF8.GetString(buffer);
string[] sections = request.Split("\r\n");

string[] requestLine = sections[0].Split(' ');
string method = requestLine[0];
string url = requestLine[1];
string httpVersion = requestLine[2];

string[] urlSections = url.Split('/');
string statusMessage = "200 OK";

string? content = null;

Console.WriteLine(urlSections[1]);

switch (urlSections[1])
{
    case "":
        // do nothing
        break;
    case "echo":
        content = urlSections[2];
        break;
    default:
        statusMessage = "404 Not Found";
        break;
}

StringBuilder b = new();
b.Append($"{httpVersion} {statusMessage}\r\n");

if (content != null)
{
    b.Append("Content-Type: text/plain\r\n");
    b.Append($"Content-Length: {content.Length}\r\n");
}
b.Append("\r\n");
if (content != null)
{
    b.Append(content);
    Console.WriteLine(content);
}

socket.Send(Encoding.UTF8.GetBytes(b.ToString()));
