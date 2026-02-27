using System;

namespace Email.Models
{
    public class ApiToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public int AccountId { get; set; }
        public bool Revoked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}