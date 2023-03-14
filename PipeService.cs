using Microsoft.Extensions.Hosting;
using PipeConnector.Enums;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeConnector
{
    public class PipeService : IDisposable
    {
        private NamedPipeClientStream? _readStream;
        private NamedPipeServerStream? _writeStream;

        public Action<PipeService, object>? MessageRecieved;
        public Action<PipeService, object>? ConnectionEstablished;

        //connect to pipes
        public void EstablishConnection(PipeType clientPipe, PipeType serverPipe, ConsumerType consumerType, Action<PipeService, object> messageRecieved, Action<bool,string> connected)
        {
            MessageRecieved = messageRecieved;

            try
            {
                Task.Run(async () => { await ConnectPipesAsync(clientPipe.ToString(), serverPipe.ToString(), consumerType); connected.Invoke(true, "OK"); await ReadAsync(); });
            }
            catch(Exception ex)
            {
                connected.Invoke(false,ex.Message);
            }
        }
       
        public ConfirmType SendMessage(object message)
        {
            if (_writeStream != null && _writeStream.IsConnected)
            {
                var sw = new StreamWriter(_writeStream);

                try
                {
                    sw.AutoFlush = true;

                    sw.WriteLine(message);

                    _writeStream?.WaitForPipeDrain();

                    return ConfirmType.OK;
                }
                catch
                {
                    return ConfirmType.NOK;
                }
            }

            return ConfirmType.NOK;
        }
        private async Task ConnectPipesAsync(string clientStreamName, string serverStreamName, ConsumerType consumerType)
        {
            if (consumerType == ConsumerType.client)
            {
                _writeStream = new NamedPipeServerStream(serverStreamName, PipeDirection.Out);
                await _writeStream.WaitForConnectionAsync();

                _readStream = new NamedPipeClientStream(".", clientStreamName, PipeDirection.In);
                _readStream.Connect();
            }
            else
            {
                _readStream = new NamedPipeClientStream(".", serverStreamName, PipeDirection.In);
                _readStream.Connect();

                _writeStream = new NamedPipeServerStream(clientStreamName, PipeDirection.Out);
                await _writeStream.WaitForConnectionAsync();
            }
        }
        private async Task ReadAsync()
        {
            if (_readStream != null)
            {
                var sr = new StreamReader(_readStream);

                string? temp;
                while ((temp = sr.ReadLine()) != null)
                {
                    MessageRecieved?.Invoke(this, temp);
                }

                await Task.Delay(1000);
            }            
        }

        public void Dispose()
        {
            _readStream?.Dispose();
            _writeStream?.Dispose();
        }
        public void DisposeConnection()
        {
            _readStream?.Dispose();
            _writeStream?.Dispose();
        }
    }
}
