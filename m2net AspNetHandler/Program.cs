using System;
using System.IO;
using Cassini;

namespace m2net.AspNetHandler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 5)
            {
                Console.WriteLine("usage: m2net.AspNetHandler.exe senderId subAddr pudAddr physicalDir virtualDir");
                return;
            }

            string sender_id = args[0];
            string sub_addr = args[1];
            string pub_addr = args[2];
            string dir = args[3];
            string virt = args[4];

            if (!Directory.Exists(dir))
            {
                Console.WriteLine("Physical directory '{0}' does not exist.", dir);
                return;
            }

            Connection conn;
            try
            {
                conn = new Connection(sender_id, sub_addr, pub_addr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not create Mongrel2 connection: " + ex.Message);
                return;
            }

            Server srv;
            try
            {
                srv = new Server(conn, virt, dir);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Could not create ASP.NET server: " + ex.Message);
                return;
            }

            srv.Start();

            Console.WriteLine("Press enter to exit m2net.AspNetHandler.");
            Console.ReadLine();

            srv.Stop();
        }
    }
}
