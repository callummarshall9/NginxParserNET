using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NginxGUI.Entities;

namespace NginxGUI.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public DbSet<NginxProxyService> NginxProxyServices { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}