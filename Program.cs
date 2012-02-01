using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HttpParser.BNF;
using System.Collections.Specialized;

namespace HttpParser
{
    class Program
    {
        public static void Main()
        {
            string content =
@"POST    HTTP://LIULIJIN.INFO     HTTP/1.1
HOST:LIULIJIN.INFO
Cookie:s=1

xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
";
            CodeTimer.Initialize();
            CodeTimer.Time("!", 50000, () =>
            {
                HttpParser parser = new HttpParser();
                parser.Go(content);
            });

            Console.ReadLine();
        }

        private static RequestLineTokens requestLine;
    }

    public class HttpParser
    {
        RequestLineTokens lineTokens;
        RequestHeaderTokens headerTokens;
        public void Go(string content)
        {
            lineTokens = new RequestLineTokens(content);
            headerTokens = new RequestHeaderTokens(content);

            for (int i = 0; i < content.Length; i++) {
                switch (State) {
                    case ParseState.LineTokens:
                        ParseLine(content[i],ref i);
                        break;
                    case ParseState.Headers:
                        ParseHeader(content[i], ref i);
                        break;
                    case ParseState.Message:
                        break;
                    default:
                        break;
                }
            }
        }

        private void ParseLine(char p, ref int i)
        {
            var result = lineTokens.InputToken(p, i);
            switch (result) {
                case DFAResult.Continue:
                    break;
                case DFAResult.Quit:
                    AssignRequestLine();
                    break;
                case DFAResult.ElseQuit:
                    AssignRequestLine();
                    i--;
                    break;
                case DFAResult.End:
                    AssignRequestLine();
                    State = ParseState.Headers;
                    headerTokens.Output = new TokensOutput();
                    headerTokens.Output.OutputIndex = i + 1;
                    break;
                default:
                    break;
            }
        }

        private void AssignRequestLine()
        {
            switch (lineTokens.Output.TokenType) {
                case SyntaxToken.Method:
                    OutputRequest.Method = lineTokens.Output.Text;
                    break;
                case SyntaxToken.Uri:
                    OutputRequest.Uri = lineTokens.Output.Text;
                    break;
                case SyntaxToken.Version:
                    OutputRequest.Version = lineTokens.Output.Text;
                    break;
            }
        }

        private string lastHeaderName = "";
        private void ParseHeader(char p, ref int i)
        {
            var result = headerTokens.InputToken(p, i);
            switch (result) {
                case DFAResult.Continue:
                    break;
                case DFAResult.Quit:
                    AssignHeaders();
                    break;
                case DFAResult.ElseQuit:
                    AssignHeaders();
                    i--;
                    break;
                case DFAResult.End:
                    AssignHeaders();
                    State = ParseState.Message;
                    break;
            }
        }

        private void AssignHeaders()
        {
            if (OutputRequest.Headers == null)
                OutputRequest.Headers = new NameValueCollection();

            switch (headerTokens.Output.TokenType) {
                case SyntaxToken.HearerName:
                    lastHeaderName = headerTokens.Output.Text;
                    OutputRequest.Headers.Add(headerTokens.Output.Text, String.Empty);
                    break;
                case SyntaxToken.HeadValue:
                    OutputRequest.Headers[lastHeaderName] = headerTokens.Output.Text;
                    break;
            }
        }

        enum ParseState
        {
            LineTokens,
            Headers,
            Message,
        }
        private ParseState State = ParseState.LineTokens;
        public RequestEntity OutputRequest = new RequestEntity();

    }

    public class RequestEntity
    {
        public string Method { get; set; }
        public string Version { get; set; }
        public string Uri { get; set; }
        public NameValueCollection Headers { get; set; }
    }
}
