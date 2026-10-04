using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TikTokGPMTool.Data.Enitities;
namespace TikTokGPMTool.Data
{
	public class TeleDataContext : DbContext
	{
		public TeleDataContext(DbContextOptions<TeleDataContext> options)
		   : base(options)
		{
		}
		public DbSet<Account> Accounts { get; set; }
		public DbSet<TikTokActionHistory> TikTokActionHistories { get; set; }
		public DbSet<FacebookGroupScanHistory> FacebookGroupScanHistories { get; set; }
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Account>(entity =>
			{
				entity.HasKey(p => p.AccountIDKey);
				entity.Property(p => p.AccountIDKey).ValueGeneratedOnAdd();
				entity.Property(a => a.FullAccount);
				entity.Property(a => a.Email);
				entity.Property(a => a.PhoneNumber);
				entity.HasIndex(a => a.PassEmail);
				entity.Property(a => a.Cookie);
				entity.Property(a => a.Status);
				entity.Property(a => a.Proxy);
				entity.Property(a => a.UserAgent);
				entity.Property(a => a.GPMID);
			});
			modelBuilder.Entity<TikTokActionHistory>(entity =>
			{
				entity.HasKey(x => x.Id);
				entity.HasIndex(x => new { x.GpmProfileId, x.VideoId, x.ActionType }).IsUnique();
				entity.Property(x => x.GpmProfileId).IsRequired();
				entity.Property(x => x.VideoId).IsRequired();
			});
			modelBuilder.Entity<FacebookGroupScanHistory>(entity =>
			{
				entity.HasKey(x => x.Id);
				entity.HasIndex(x => new { x.GroupKey, x.FacebookUid }).IsUnique();
				entity.Property(x => x.GroupKey).IsRequired();
				entity.Property(x => x.GpmProfileId).IsRequired();
				entity.Property(x => x.FacebookUid).IsRequired();
			});
		}
	}
}
