using System;
using System.Linq;

namespace Server.Classes
{
    public class Client
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token {  get; set; }
        public DateTime DateConnect {  get; set; }
        public bool IsBlacklisted { get; set; }
        
        public Client()
        {
            CreateToken();
            DateConnect = DateTime.Now;
        }
        public Client(int id, string username, string password )
        {
            Id = id;
            Username = username;
            Password = password;
            DateConnect = DateTime.Now;
            CreateToken();
            IsBlacklisted = false;
        }

        public void CreateToken()
        {
            Random random = new Random();
            string Chars = "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm0123456789";
            this.Token = new string(Enumerable.Repeat(Chars, 15).Select(x => x[random.Next(Chars.Length)]).ToArray());
        }
    }
}
