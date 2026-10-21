using System;
using System.Collections.Concurrent;

namespace VyacheslavsMegaWebApp.Service
{
    public class LaunchTicket
    {
        public string TicketId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Hwid { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }

    public class LaunchTicketService
    {
        private readonly ConcurrentDictionary<string, LaunchTicket> _tickets = new(StringComparer.OrdinalIgnoreCase);

        public LaunchTicket CreateTicket(string username, string hwid, TimeSpan? ttl = null)
        {
            CleanupExpired();

            var ticket = new LaunchTicket
            {
                TicketId = Guid.NewGuid().ToString("N"),
                Username = username.Trim(),
                Hwid = hwid.Trim(),
                ExpiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromMinutes(15)),
                IsUsed = false
            };

            _tickets[ticket.TicketId] = ticket;
            return ticket;
        }

        public bool HasActiveTicketForUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            CleanupExpired();
            var target = username.Trim();
            return _tickets.Values.Any(t => !t.IsUsed && t.ExpiresAt > DateTime.UtcNow && t.Username.Equals(target, StringComparison.OrdinalIgnoreCase));
        }

        public bool ValidateAndConsumeTicket(string ticketId, string username)
        {
            CleanupExpired();

            if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(username))
                return false;

            if (_tickets.TryGetValue(ticketId.Trim(), out var ticket))
            {
                if (ticket.IsUsed || ticket.ExpiresAt < DateTime.UtcNow)
                {
                    _tickets.TryRemove(ticketId.Trim(), out _);
                    return false;
                }

                if (!ticket.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                ticket.IsUsed = true;
                _tickets.TryRemove(ticketId.Trim(), out _);
                return true;
            }

            return false;
        }

        private void CleanupExpired()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _tickets)
            {
                if (kvp.Value.ExpiresAt < now || kvp.Value.IsUsed)
                {
                    _tickets.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
