using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Dispatching; // 如果是 MAUI
using Ionic.Zlib;
namespace PhotoProcess
{
    public class TcpHostService
    {
        private readonly int _port;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        // 当前连接客户端
        public TcpClient? CurrentClient { get; private set; }

        // 事件：接收到图片文件
        public event Action<string>? ImageReceived;

        // 事件：客户端连接成功
        public event Action<TcpClient>? ClientConnected;

        // 事件：客户端断开连接
        public event Action<TcpClient>? ClientDisconnected;

        public TcpHostService(int port = 12345)
        {
            _port = port;
        }

        public void Start()
        {
            if (_listener != null) return;
            _cts = new CancellationTokenSource();

            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            _ = Task.Run(() => AcceptLoop(_cts.Token));

            Console.WriteLine($"TCP 已监听端口 {_port}");
        }

        public void Stop()
        {
            _cts?.Cancel();
            CurrentClient?.Close();
            _listener?.Stop();
            _listener = null;
            CurrentClient = null;
        }

        private async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync(token);
                    Console.WriteLine($"客户端已连接: {client.Client.RemoteEndPoint}");
                    CurrentClient = client;

                    // 🔔 触发客户端连接事件
                    ClientConnected?.Invoke(client);

                    // 启动心跳发送线程
                    _ = Task.Run(() => HeartbeatLoop(client, token), token);

                    // 启动接收图片线程
                    _ = Task.Run(() => ReceiveLoop(client, token), token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Console.WriteLine($"Accept error: {ex.Message}"); }
            }
        }

        private async Task HeartbeatLoop(TcpClient client, CancellationToken token)
        {
            var stream = client.GetStream();
            try
            {
                while (!token.IsCancellationRequested && client.Connected)
                {
                    byte[] hb = Encoding.UTF8.GetBytes("A");
                    await stream.WriteAsync(hb, 0, hb.Length, token);
                    await stream.FlushAsync(token);
                    await Task.Delay(5000, token);
                }
            }
            catch { }
        }

        private async Task ReceiveLoop(TcpClient client, CancellationToken token)
        {
            var stream = client.GetStream();
            try
            {
                while (!token.IsCancellationRequested && client.Connected)
                {
                    // 4字节长度 -> header 长度
                    byte[] lenBuf = new byte[4];
                    await ReadExact(stream, lenBuf, token);

                    int headerLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenBuf, 0));
                    if (headerLen <= 0 || headerLen > 10240)
                    {
                        Console.WriteLine("非法 header 长度，断开连接");
                        break;
                    }

                    byte[] headerBuf = new byte[headerLen];
                    await ReadExact(stream, headerBuf, token);

                    var header = JsonSerializer.Deserialize<FileHeader>(Encoding.UTF8.GetString(headerBuf))!;
                    long compressedSize = header.size;

                    if (compressedSize <= 0 || compressedSize > 50 * 1024 * 1024) // 限制 50MB
                    {
                        Console.WriteLine("非法文件大小，断开连接");
                        break;
                    }

                    byte[] compressedData = new byte[compressedSize];
                    await ReadExact(stream, compressedData, token);

                    byte[] imageData;
                    try
                    {
                        imageData = DecompressZlib(compressedData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"解压失败: {ex.Message}");
                        continue;
                    }

                    string savePath = Path.Combine(Path.GetTempPath(),
                        $"{DateTime.Now:yyyyMMdd_HHmmss}_{header.filename}");

                    await File.WriteAllBytesAsync(savePath, imageData, token);

                    // 🔔 在主线程安全触发事件（适用于 MAUI/WPF）
                    MainThread.BeginInvokeOnMainThread(() => ImageReceived?.Invoke(savePath));
                    Console.WriteLine($"图片已保存: {savePath}");
                }
            }
            catch (IOException)
            {
                Console.WriteLine("客户端断开连接");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收异常: {ex}");
            }
            finally
            {

            }
        }

        private static byte[] DecompressZlib(byte[] compressed)
        {
            using var ms = new MemoryStream(compressed);
            using var zlibStream = new ZlibStream(ms, CompressionMode.Decompress);
            using var outMs = new MemoryStream();
            zlibStream.CopyTo(outMs);
            return outMs.ToArray();
        }

        private static async Task<int> ReadExact(NetworkStream stream, byte[] buffer, CancellationToken token)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), token);
                if (read <= 0) throw new IOException("连接断开");
                offset += read;
            }
            return offset;
        }

        // 发送拍照指令
        public async Task SendCaptureCommandAsync()
        {
            if (CurrentClient?.Connected != true) return;
            var stream = CurrentClient.GetStream();
            byte[] cmd = Encoding.UTF8.GetBytes("P\n");
            await stream.WriteAsync(cmd, 0, cmd.Length);
            await stream.FlushAsync();
            Console.WriteLine("已发送 'P' 指令");
        }

        private record FileHeader(string type, string filename, long size);
    }
}
