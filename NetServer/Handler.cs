using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace netserver
{
    class Handler
    {
        private static int handlerCounter = 0;
        private static int handlerUsage = 0;
        private int handlerId;


        private TcpClient tcpClient;
        private string documentRoot;
        private Dictionary<string, string> typeMap = new Dictionary<string, string>();
        private IDispatch dispatcher;
        private Stopwatch stopWatch = new Stopwatch();

        private enum RequestType
        {
            UNDEFINED,
            GET,
            POST
        };

        public Handler(Server server, IDispatch dispatcher, TcpClient tcpClient) : base()
        {
            handlerUsage++;
            handlerId = (++handlerCounter);

            //Console.WriteLine("Started Handler: " + handlerId + " (usage: " + handlerUsage + ")");

            this.dispatcher = dispatcher;
            this.tcpClient = tcpClient;

            this.documentRoot = server.getRootDirectory();

            typeMap["html"] = "text/html; charset=utf-8";
            typeMap["txt"] = "text/text; charset=utf-8";
            typeMap["js"] = "text/javascript; charset=utf-8";
            typeMap["css"] = "text/css; charset=utf-8";
            typeMap["png"] = "image/png";
            typeMap["ico"] = "image/ico";
        }

        public void run()
        {
            //Console.WriteLine("NEW CONNECTION!");

            byte[] buffer = new byte[32768];

            var stream = tcpClient.GetStream();

            int timeout = 5000;

            this.tcpClient.ReceiveTimeout = timeout;
            stream.ReadTimeout = timeout;

            try
            {
                for (;;)
                {
                    int count = stream.Read(buffer, 0, buffer.Length);
                    if (count < 1)
                    {
                        break;
                    }

                    System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
                    string request = enc.GetString(buffer, 0, count);

                    //Console.Out.WriteLine("---- REQUEST START");

                    stopWatch.Restart();

                    bool keepAlive = handleRequest(request);

                    stopWatch.Stop();

                    //Console.Out.WriteLine("---- REQUEST END (" + stopWatch.ElapsedMilliseconds + "ms)");

                    if (false == keepAlive)
                    {
                        break;
                    }
                }
            }
            catch (IOException)
            {
                //Console.Out.WriteLine("IOException " + e);
            }

            stream.Close();
            stream = null;

            tcpClient.Close();
            tcpClient = null;

            handlerUsage--;
            //Console.WriteLine("Finished Handler: " + handlerId + " (usage: " + handlerUsage + ")");

        }

        private bool handleRequest(string request)
        {
            if (null == request || request.Length == 0) return false;

            string[] lines = request.Split(new char[] { '\n' }, StringSplitOptions.None);
            if (lines.Length == 0) return false;

            string requestCmd = lines[0];
            RequestType requestType = RequestType.UNDEFINED;
            string requestUrl = "";

            string[] tokens = requestCmd.Split(new char[] { ' ', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 2)
            {
                if (tokens[0].Equals("GET"))
                {
                    requestType = RequestType.GET;
                }
                else if (tokens[0].Equals("POST"))
                {
                    requestType = RequestType.POST;
                }

                requestUrl = tokens[1];
            }

            int postData = 0;

            Dictionary<string, string> param = new Dictionary<string, string>();
            for (int i=1; i<lines.Length; i++)
            {
                string currentLine = lines[i].Trim();
                if (currentLine.Length < 1)
                {
                    postData = i+1;
                    break;
                }

                int pos = currentLine.IndexOf(':');
                if (pos > 0)
                {
                    string key = currentLine.Substring(0, pos).Trim();
                    string value = currentLine.Substring(pos + 1).Trim();
                    if (false == param.Keys.Contains(key))
                    {
                        param.Add(key, value);
                    }
                    else
                    {
                        param[key] = value;
                    }
                }
            }

            if (requestType == RequestType.GET)
            {
                if (false == handleGet(requestUrl, param))
                {
                    return false;
                }
            }
            else if (requestType == RequestType.POST)
            {
                string postParam = lines[postData];
                if (false == handlePost(requestUrl, param, postParam))
                {
                    return false;
                }
            }

            bool keepAlive = false;
            string keepAliveValue;
            if (param.TryGetValue("Connection", out keepAliveValue))
            {
                keepAlive = keepAliveValue.Equals("Keep-Alive", StringComparison.CurrentCultureIgnoreCase);
            }

            //Console.Out.WriteLine("KEEP ALIVE: " + keepAlive);

            return keepAlive;
        }

        private bool handlePost(string requestUrl, Dictionary<string, string> param, string data)
        {
            if (null != dispatcher)
            {
                string command = (requestUrl.StartsWith("/")) ? requestUrl.Substring(1) : requestUrl;

                Object result = dispatcher.dispatch(command, data);

                if (null != result)
                {
                    if (result is string)
                    {
                        sendResponse((string)result, "text/text; charset=utf-8");
                        // Console.WriteLine("POST successfull: " + requestUrl + "/" + data);
                    }
                    else if (result is byte[])
                    {
                        sendResponse((byte[])result, "application/octet-stream");
                        // Console.WriteLine("POST successfull: " + requestUrl + "/ binary response");
                    }
                }
            }

            return true;
        }

        private bool handleGet(string requestUrl, Dictionary<string, string> param)
        {
            //Console.Out.WriteLine("GET request: " + requestUrl);

            string url = requestUrl;
            if (requestUrl == "/") url = "/index.html";

            string requestPath;

            if (url.StartsWith("/"))
            {
                requestPath = documentRoot + url.Replace('/', '\\');
            }
            else
            {
                requestPath = documentRoot + "\\" + url.Replace('/', '\\');
            }

            int posExtension = url.LastIndexOf('.');

            string extension;
            if (posExtension >= 0)
            {
                extension = url.Substring(posExtension+1);
            }
            else
            {
                extension = "";
            }

            if (extension == "html" ||
                extension == "txt" ||
                extension == "js" ||
                extension == "css" )
            {
                string content = readTextFile(requestPath);
                if (null == content)
                {
                    sendErrResponse();
                    return false;
                }

                sendResponse(content, typeMap[extension]);
            }
            
            else if (extension == "png" ||
                     extension == "ico")
            {
                byte[] content = readBinaryFile(requestPath);
                if (null == content)
                {
                    sendErrResponse();
                    return false;
                }

                sendResponse(content, typeMap[extension]);
            }

            //Console.WriteLine("GET successfull: " + requestPath);

            return true;
        }

        private string readTextFile(string path)
        {
            try
            {
                string content = System.IO.File.ReadAllText(path);
                return content;
            }
            catch (FileNotFoundException)
            {
                ;
            }

            return null;
        }

        private byte[] readBinaryFile(string path)
        {

            try
            {
                byte[] content = System.IO.File.ReadAllBytes(path);
                return content;
            }
            catch (FileNotFoundException)
            {
                ;
            }

            return null;
        }

        private void sendErrResponse()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("HTTP/1.1 404 Not Found");
            sb.AppendLine("Connection: Close");
            sb.Append("\r\n");
            sb.AppendLine("<html><body>Not found!</body></html>");
            sb.Append("\r\n");

            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            byte[] responseMessage = enc.GetBytes(sb.ToString());

            try
            {
                tcpClient.GetStream().Write(responseMessage, 0, responseMessage.Length);
            }
            catch (IOException)
            {
                ;
            }
        }

        private void sendResponse(string content, string type)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("HTTP/1.1 200 OK");
            sb.AppendLine("Content-Language: de");
            sb.AppendLine("Content-Type: " + type);
            sb.AppendLine("Content-Length: " + content.Length);
            sb.AppendLine("Connection: Keep-Alive");
            sb.Append("\r\n");
            sb.AppendLine(content);
            sb.Append("\r\n");

            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            byte[] responseMessage = enc.GetBytes(sb.ToString());

            try
            {
                tcpClient.GetStream().Write(responseMessage, 0, responseMessage.Length);
                //Console.WriteLine("Sent response: " + responseMessage.Length + " chars");
            }
            catch (IOException)
            {
                ;
            }

        }

        private void sendResponse(byte[] content, string type)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("HTTP/1.1 200 OK");
            sb.AppendLine("Content-Language: de");
            sb.AppendLine("Content-Type: " + type);
            sb.AppendLine("Content-Length: " + content.Length);
            sb.AppendLine("Connection: Keep-Alive");
            sb.Append("\r\n");

            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            byte[] responseMessage = enc.GetBytes(sb.ToString());

            try
            {
                tcpClient.GetStream().Write(responseMessage, 0, responseMessage.Length);
                tcpClient.GetStream().Write(content, 0, content.Length);
                //Console.WriteLine("Sent response: " + (responseMessage.Length + content.Length) + " bytes");
            }
            catch (IOException)
            {
                ;
            }
        }
    }
}
