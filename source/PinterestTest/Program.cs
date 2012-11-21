using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PinterestTest
{
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Xml.Linq;
    using CsQuery;

    class Program
    {

        const string ApiLatlongFromAddress = "http://maps.googleapis.com/maps/api/geocode/xml?address={0}&sensor=false";
        private const string InputFile = "default.html";

        static void Main(string[] args)
        {
            var username = string.Empty;
            var board = string.Empty;
            var outputFile = "output.html";
            if (args.Length < 2)
            {
                Console.WriteLine("Please enter a username and board");
                Console.WriteLine("Usage: mapinterest <username> <board> [output file]");
                return;
            } else
            {
                username = args[0];
                board = args[1];
                if(args.Length >= 3)
                {
                    outputFile = args[2];
                }
            }
            GenerateMap(username, board, outputFile);
        }

        public static void GenerateMap(string username, string board, string outputFile)
        {
            Console.WriteLine("1. Getting pins...");

            var dom = new CQ();
            
            try
            {
                dom = CQ.CreateFromUrl(string.Format("http://pinterest.com/{0}/{1}/", username, board));
            } 
            catch(WebException e)
            {
                if(e.Status == WebExceptionStatus.ProtocolError)
                {
                    Console.WriteLine("User or board could not be found");
                }
                Environment.Exit(0);
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("function points() {");

            int count = dom[".pin"].Count();

            IList<string> unclearPins = new List<string>();

            Console.WriteLine("2. {0} pins found, gettings positions... (this could take a while depending on the number of pins)", count);
            Console.WriteLine();

            dom[".pin"].Each((i, e) =>
            {
                // 2 seconde slapen om limiet geocoding te voorkomen
                Thread.Sleep(2000);

                var elem = CQ.Create(e);
                var pinDesc = elem.Find("p.description").First().Text();
                var pinImg = elem.Find("img.PinImageImg").First().Attr("src");

                try
                {
                    var location = GetLatLongFromAddress(pinDesc);
                    builder.AppendFormat("var marker{0} = new google.maps.Marker({{position: new google.maps.LatLng({1}), map: map, title: '{2}'}});\n", i, location, pinDesc);
                    builder.AppendFormat("google.maps.event.addListener(marker{0}, 'click', function() {{\n", i);
                    builder.AppendFormat("    var info{0} = new google.maps.InfoWindow({{content: '<img src=\"{1}\">', disableAutoPan: true}});\n", i, pinImg);
                    builder.AppendFormat("    info{0}.open(map, marker{0});\n", i);
                    builder.AppendLine("});");

                }
                catch (Exception err)
                {
                    unclearPins.Add(pinDesc);
                }

                int percentage = (int) ((((i+1)*1d)/(count*1d))*100);
                RenderConsoleProgress(percentage, '\u2592', ConsoleColor.Green, string.Format("{0} of {1} done ({2}%)", i+1, count, percentage));
            });

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            if (unclearPins.Count > 0)
            {
                Console.WriteLine("The following pins could not be located on a map:");
                foreach (string unclearPin in unclearPins)
                {
                    Console.WriteLine(string.Format("- {0}", unclearPin));
                }
            }

            builder.AppendLine("}");

            string combine = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, InputFile);
            var doc = CQ.CreateDocumentFromFile(combine);
            doc["script#points"].Text(builder.ToString());
            
            doc.Save(outputFile);

            ConsoleColor originalColor = Console.ForegroundColor;
            Console.Write("3. Done, results written to ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(outputFile);
            Console.ForegroundColor = originalColor;
            Console.WriteLine("!");
        }

        public static string GetLatLongFromAddress(string address)
        {
            XDocument doc = XDocument.Load(string.Format(ApiLatlongFromAddress, address));
            var els = doc.Descendants("result").Descendants("geometry").Descendants("location").First();
            return string.Format("{0}, {1}", (els.Nodes().First() as XElement).Value, (els.Nodes().ElementAt(1) as XElement).Value);
        }

        public static void OverwriteConsoleMessage(string message)
        {
            Console.CursorLeft = 0;
            int maxCharacterWidth = Console.WindowWidth - 1;
            if (message.Length > maxCharacterWidth)
            {
                message = message.Substring(0, maxCharacterWidth - 3) + "...";
            }
            message = message + new string(' ', maxCharacterWidth - message.Length);
            Console.Write(message);
        }

        public static void RenderConsoleProgress(int percentage)
        {
            RenderConsoleProgress(percentage, '\u2590', Console.ForegroundColor, "");
        }

        public static void RenderConsoleProgress(int percentage, char progressBarCharacter,
                  ConsoleColor color, string message)
        {
            Console.CursorVisible = false;
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.CursorLeft = 0;
            int width = Console.WindowWidth - 1;
            int newWidth = (int)((width * percentage) / 100d);
            string progBar = new string(progressBarCharacter, newWidth) +
                  new string(' ', width - newWidth);
            Console.Write(progBar);
            if (string.IsNullOrEmpty(message)) message = "";
            Console.CursorTop++;
            OverwriteConsoleMessage(message);
            Console.CursorTop--;
            Console.ForegroundColor = originalColor;
            Console.CursorVisible = true;
        }
    }
}
