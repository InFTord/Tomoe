﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Tomoe.Models;

#nullable disable

namespace Tomoe.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20220108224222_3.0.0-alpha1-tags")]
    partial class _300alpha1tags
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Tomoe.Models.DatabaseGuildConfig", b =>
                {
                    b.Property<decimal>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.HasKey("GuildId")
                        .HasName("pk_guild_configs");

                    b.ToTable("guild_configs", (string)null);
                });

            modelBuilder.Entity("Tomoe.Models.DatabaseSnowflakePerms", b =>
                {
                    b.Property<decimal>("SnowflakeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("snowflake_id");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<int>("Permissions")
                        .HasColumnType("integer")
                        .HasColumnName("permissions");

                    b.HasKey("SnowflakeId")
                        .HasName("pk_snowflake_perms");

                    b.ToTable("snowflake_perms", (string)null);
                });

            modelBuilder.Entity("Tomoe.Models.DatabaseTag", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<List<string>>("Aliases")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("aliases");

                    b.Property<decimal>("AuthorId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("author_id");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("content");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<int>("UsageCount")
                        .HasColumnType("integer")
                        .HasColumnName("usage_count");

                    b.HasKey("Id")
                        .HasName("pk_tags");

                    b.ToTable("tags", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
