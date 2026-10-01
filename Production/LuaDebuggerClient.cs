using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Aoe4Helper.Production
{
   /// <summary>编辑器 Lua 调试协议 v6；求值完成或取消后始终发送 RUN。</summary>
   public sealed class LuaDebuggerClient
   {
      public static string Quote(string value)
      {
         var result = new StringBuilder("\"");
         foreach (char c in value)
         {
            if (c == '\\' || c == '"') result.Append('\\').Append(c);
            else if (c == '\n') result.Append("\\n");
            else if (c == '\r') result.Append("\\r");
            else if (c == '\t') result.Append("\\t");
            else if (c < 32) result.Append('\\').Append(((int)c).ToString("D3"));
            else result.Append(c);
         }
         return result.Append('"').ToString();
      }

      public async Task<string> EvaluateAsync(RpcSettings settings, string expression, CancellationToken token)
      {
         using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
         timeout.CancelAfter(TimeSpan.FromSeconds(8));
         var ct = timeout.Token;
         using var client = new TcpClient();
         try { await client.ConnectAsync(settings.Host, settings.LuaPort, ct).ConfigureAwait(false); }
         catch (OperationCanceledException) when (!token.IsCancellationRequested)
         {
            throw new TimeoutException("连接 Lua 调试器超时，请确认游戏已用 -dev 启动。");
         }
         using var stream = client.GetStream();
         bool breakSent = false;
         try
         {
            string greeting = await ReadFrameAsync(stream, ct).ConfigureAwait(false);
            if (greeting != "READY 6") throw new InvalidOperationException($"Lua 调试器握手失败：{greeting}");
            breakSent = true;
            await SendAsync(stream, "BREAK \"Scenario\"", ct).ConfigureAwait(false);
            bool stopped = false;
            while (true)
            {
               string frame = await ReadFrameAsync(stream, ct).ConfigureAwait(false);
               ThrowOnError(frame);
               if (frame.StartsWith("BREAK \"Scenario\" ", StringComparison.Ordinal)) stopped = true;
               if (stopped && frame == "READY \"Scenario\"") break;
            }
            await SendAsync(stream, "ADDWATCH 71231 " + Quote(expression), ct).ConfigureAwait(false);
            await SendAsync(stream, "GETWATCH 0 71231", ct).ConfigureAwait(false);
            while (true)
            {
               string frame = await ReadFrameAsync(stream, ct).ConfigureAwait(false);
               ThrowOnError(frame);
               if (frame.StartsWith("WATCHERR 71231 ", StringComparison.Ordinal))
                  throw new InvalidOperationException(Unquote(frame.Substring("WATCHERR 71231 ".Length).Trim()));
               // 所有桥接表达式返回字符串。格式：WATCH id type raw aux "value" "display"。
               var match = Regex.Match(frame, "^WATCH 71231 4 \\S+ \\S+ (\"(?:\\\\.|[^\"\\\\])*\")");
               if (match.Success) return Unquote(match.Groups[1].Value);
               if (frame.StartsWith("WATCH 71231 ", StringComparison.Ordinal))
                  throw new InvalidOperationException("Lua 桥接表达式未返回字符串。");
            }
         }
         catch (OperationCanceledException) when (!token.IsCancellationRequested)
         {
            throw new TimeoutException("Lua 调试器未响应，请确认已用 -dev 启动并进入未暂停的单人对局。");
         }
         finally
         {
            if (breakSent)
            {
               // 两项清理分别计时，移除监视项超时后仍尝试恢复脚本。
               using var removeWatch = new CancellationTokenSource(TimeSpan.FromSeconds(2));
               try { await SendAsync(stream, "REMWATCH 71231", removeWatch.Token).ConfigureAwait(false); }
               catch (Exception ex) when (ex is IOException || ex is OperationCanceledException || ex is SocketException) { }
               using var resume = new CancellationTokenSource(TimeSpan.FromSeconds(2));
               try
               {
                  await SendAsync(stream, "RUN \"Scenario\"", resume.Token).ConfigureAwait(false);
                  while (await ReadFrameAsync(stream, resume.Token).ConfigureAwait(false) != "RUN \"Scenario\"") { }
               }
               catch (Exception ex) when (ex is IOException || ex is OperationCanceledException || ex is SocketException) { }
            }
         }
      }

      private static void ThrowOnError(string frame)
      {
         if (frame.StartsWith("ERR ", StringComparison.Ordinal) || frame.StartsWith("REFUSED", StringComparison.Ordinal))
            throw new InvalidOperationException(frame);
      }

      private static async Task SendAsync(NetworkStream stream, string command, CancellationToken token)
      {
         byte[] data = Encoding.UTF8.GetBytes(command + "\r");
         await stream.WriteAsync(data, token).ConfigureAwait(false);
      }

      private static async Task<string> ReadFrameAsync(NetworkStream stream, CancellationToken token)
      {
         byte[] header = new byte[8];
         await stream.ReadExactlyAsync(header, token).ConfigureAwait(false);
         ulong size = BinaryPrimitives.ReadUInt64LittleEndian(header);
         if (size > 16 * 1024 * 1024) throw new IOException("Lua 调试器返回了无效的数据帧长度。");
         byte[] body = new byte[(int)size];
         await stream.ReadExactlyAsync(body, token).ConfigureAwait(false);
         return Encoding.UTF8.GetString(body);
      }

      private static string Unquote(string text)
      {
         if (text.Length < 2 || text[0] != '"' || text[text.Length - 1] != '"') return text;
         var result = new StringBuilder();
         for (int i = 1; i < text.Length - 1; i++)
         {
            char c = text[i];
            if (c != '\\' || i + 1 >= text.Length - 1) { result.Append(c); continue; }
            c = text[++i];
            if (c >= '0' && c <= '9')
            {
               int number = c - '0';
               for (int count = 1; count < 3 && i + 1 < text.Length - 1 && char.IsAsciiDigit(text[i + 1]); count++)
                  number = number * 10 + text[++i] - '0';
               result.Append((char)number);
            }
            else result.Append(c switch { 'n' => '\n', 'r' => '\r', 't' => '\t', 'b' => '\b', 'f' => '\f', _ => c });
         }
         return result.ToString();
      }
   }
}
