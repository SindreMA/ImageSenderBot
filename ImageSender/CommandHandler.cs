using Discord;
using Discord.Addons.EmojiTools;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UtilityBot.Services.Configuration;
using UtilityBot.Services.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using MimeKit;
using MailKit.Net.Smtp;

namespace UtilityBot
{
    public class CommandHandler
    {

        private readonly IServiceProvider _provider;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly Config _config;
        private readonly ILogger _logger;
        public static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
        public CommandHandler(IServiceProvider provider)
        {
            _provider = provider;
            _client = _provider.GetService<DiscordSocketClient>();
            _client.MessageReceived += _client_MessageReceived;
            _commands = _provider.GetService<CommandService>();
            var log = _provider.GetService<LogAdaptor>();
            _commands.Log += log.LogCommand;
            _config = _provider.GetService<Config>();
            _logger = _provider.GetService<Logger>().ForContext<CommandService>();


        }
        public void SendMailFile(Attachment image)
        {
            var imagefile = "";
            try
            {


                string EmailUsername = "";
                string EmailPassword = "";
                string EmailSMTPServer = "";
                string EmailAdresse = "";
                int EmailSMTPPort = 0;
                bool EmailUsingSSL = false;
                string EmailUploadAdresse = "";

                try
                {
                    Console.WriteLine("Reading Email Settings");
                    dynamic json = JsonConvert.DeserializeObject(File.ReadAllText("EmailSettings.json"));
                    EmailUsername = json.EmailUsername;
                    EmailPassword = json.EmailPassword;
                    EmailSMTPServer = json.EmailSMTPServer;
                    EmailSMTPPort = json.EmailSMTPPort;
                    EmailUsingSSL = json.EmailUsingSSL;
                    EmailAdresse = json.EmailAdresse;
                    EmailUploadAdresse = json.EmailUploadAdresse;
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("error at getting email settings");

                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Image Sender", EmailAdresse));
                message.To.Add(new MailboxAddress("Image Receiver", EmailUploadAdresse));
                message.Subject = "Image Upload";

                message.Body = new TextPart("plain")
                {
                    Text = "Just a simple image upload."
                };

                imagefile = DownloadFile(image);
                Console.WriteLine("Downloading image");

                var builder = new BodyBuilder();
                builder.TextBody = "Just a simple image upload.";
                Console.WriteLine("Adding image to mail.");
                builder.Attachments.Add(imagefile);
                message.Body = builder.ToMessageBody();
                Console.WriteLine("Sending email.");
                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    client.Connect(EmailSMTPServer, EmailSMTPPort, EmailUsingSSL);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.Authenticate(EmailUsername, EmailPassword);
                    client.Send(message);
                    client.Disconnect(true);

                }
                File.Delete(imagefile);
                Console.WriteLine("Message was sent!");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message+ Environment.NewLine);
                File.Delete(imagefile);
            }
        }
        private Task _client_MessageReceived(SocketMessage arg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if (arg.Attachments.Count > 0)
            {
                foreach (var file in arg.Attachments)
                {
                    if (ImageExtensions.Contains(Path.GetExtension(file.Filename).ToUpperInvariant()))
                    {
                        SendMailFile(file);


                    }

                }
            }

            return null;

        }
        public string DownloadFile(Attachment file)
        {
            Random s = new Random();
            var he = file.Filename.Split('.');
            string SavePath = "File" + s.Next(1000, 999999) + "." + he[he.Count() - 1];
            using (var cli = new HttpClient())
            {
                var rslt = cli.GetAsync(file.Url).GetAwaiter().GetResult();
                if (rslt.IsSuccessStatusCode)
                {
                    var dat = rslt.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    File.WriteAllBytes(SavePath, dat);
                }
            }
            return SavePath;
        }
        public async Task ConfigureAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }
    }
}
