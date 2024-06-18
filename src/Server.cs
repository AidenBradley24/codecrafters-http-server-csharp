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

string message = url == "/" ? "200 OK" : "404 Not Found";
string response = $"{httpVersion} {message}\r\n\r\n";
Console.WriteLine(response);
socket.Send(Encoding.UTF8.GetBytes(response));
