using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Security.Policy;
using System.Web;


namespace ConsoleApplication1
{
    internal class Program
    {
        private static ILog logger = LogManager.GetLogger(typeof(Program));
        static readonly object syncRoot = new object();
        private static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            string basepath = "C:\\Users\\admin\\Desktop\\switch\\";
            string content = string.Empty;
            using (StreamReader reader = new StreamReader(basepath + "switch.html"))
            {
                content = reader.ReadToEnd();
            }
            HashSet<string> unsubscribedemails;
            using (StreamReader reader = new StreamReader(basepath + "unsub.csv"))
            {
                unsubscribedemails = new HashSet<string>();
                string line = string.Empty;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (!unsubscribedemails.Contains(line))
                    {
                        unsubscribedemails.Add(line);
                    }
                }
            }
            int count = 0;
            string filename = args[0];
            List<string> emailaddresses = new List<string>();
            using (StreamReader reader = new StreamReader(basepath + filename))
            {
                string line = string.Empty;
                while ((line = reader.ReadLine()) != null)
                {
                    emailaddresses.Add(line);
                }
            }
            var sw = Stopwatch.StartNew();
            Parallel.ForEach(emailaddresses.AsParallel(), new ParallelOptions { MaxDegreeOfParallelism = 8 }, line =>
                {
                    if (!unsubscribedemails.Contains(line))
                    {
                        try
                        {
                            SmtpClient objSmtpClient = new SmtpClient("email-smtp.us-east-1.amazonaws.com", 587);
                            objSmtpClient.EnableSsl = true;
                            objSmtpClient.Credentials = new NetworkCredential("ABCDEFGH", "ABCDEFGHpassword");
                            MailMessage objMailMessage = new MailMessage();
                            objMailMessage.From = new MailAddress("abc<no-reply@abc.com>");
                            objMailMessage.To.Add(line);
                            objMailMessage.Subject = "2 special offers for you and your friends!";
                            AlternateView objAlternateView = AlternateView.CreateAlternateViewFromString(content.Replace("*||email_address||*", HttpUtility.UrlEncode(line)), Encoding.UTF8, "text/html");
                            objMailMessage.AlternateViews.Add(objAlternateView);
                            var stopwatch = Stopwatch.StartNew();
                            objSmtpClient.Send(objMailMessage);
                            logger.Warn("Count: " + count + ", Mail sent for " + line + "Elapsed ms: " + stopwatch.ElapsedMilliseconds);
                            lock (syncRoot) count++;
                        }
                        catch (Exception ex)
                        {
                            logger.Error("For Emailaddress: [" + line + "]", ex);
                        }
                    }
                });

            logger.Warn("Task completed after ms: " + sw.ElapsedMilliseconds);
        }
    }
}
