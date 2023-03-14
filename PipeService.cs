using PipeConnector.Enums;
using System.IO.Pipes;


namespace PipeConnector
{
    /// <summary>
    /// Manage named pipes for easy communication.
    /// </summary>
    public class PipeService : IDisposable
    {
        private NamedPipeClientStream? _readStream;
        private NamedPipeServerStream? _writeStream;

        //invoked when message from other side is recieved
        public Action<PipeService, object>? MessageRecieved;

        /// <summary>
        /// Create connection for writing and process for reading. 
        /// If there is only one client/server, both sides use same client/server pipe identifiers e.g. clientPipe/serverPipe = clientPipe0/serverPipe0 
        /// </summary>
        /// <param name="clientPipe">identifier of writing pipe</param>
        /// <param name="serverPipe">identifier of reading pipe</param>
        /// <param name="consumerType">identifier of side which call this func</param>
        /// <param name="messageRecieved">invoked when message from opposite side is recieved</param>
        /// <param name="connected">invoked when the connection was successfully established</param>
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
       
        /// <summary>
        /// Send message to other side.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>if sending was not succesful, return NOK, else OK</returns>
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

        /// <summary>
        /// Sets connections of pipes.
        /// </summary>
        /// <param name="clientStreamName"></param>
        /// <param name="serverStreamName"></param>
        /// <param name="consumerType"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Process of reading.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// IDisposable
        /// </summary>
        public void Dispose()
        {
            _readStream?.Dispose();
            _writeStream?.Dispose();
        }

    }
}
