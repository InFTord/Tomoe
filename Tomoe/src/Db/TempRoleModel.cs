using System;
using System.ComponentModel.DataAnnotations;

namespace Tomoe.Db
{
    public class TempRoleModel
    {
        [Key]
        public Guid Id { get; init; }
        public ulong GuildId { get; init; }
        public ulong RoleId { get; init; }
        public ulong Assignee { get; init; }
        public ulong Assigner { get; init; }
        public DateTime ExpiresAt { get; init; }
    }
}