﻿// <auto-generated />
using System;
using DIAMConfiguration.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DIAMConfiguration.Migrations
{
    [DbContext(typeof(DIAMConfigurationDataStoreDbContext))]
    [Migration("20240214205335_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("DIAMConfiguration.Data.HostConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("Deleted")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Hostname")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("HostConfig");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Created = new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(3290), new TimeSpan(0, -8, 0, 0, 0)),
                            Hostname = "locahost"
                        });
                });

            modelBuilder.Entity("DIAMConfiguration.Data.LoginConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("Deleted")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("FormControl")
                        .HasColumnType("text");

                    b.Property<string>("FormList")
                        .HasColumnType("text");

                    b.Property<int?>("HostConfigId")
                        .HasColumnType("integer");

                    b.Property<string>("Idp")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Notification")
                        .HasColumnType("text");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("HostConfigId");

                    b.ToTable("LoginConfig");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Created = new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(2766), new TimeSpan(0, -8, 0, 0, 0)),
                            FormControl = "",
                            FormList = "",
                            Idp = "ADFS",
                            Name = "BCPS iKey",
                            Notification = "",
                            Type = "BUTTON"
                        },
                        new
                        {
                            Id = 2,
                            Created = new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(2910), new TimeSpan(0, -8, 0, 0, 0)),
                            FormControl = "",
                            FormList = "",
                            Idp = "oidcazure",
                            Name = "BCPS IDIR",
                            Notification = "",
                            Type = "BUTTON"
                        },
                        new
                        {
                            Id = 3,
                            Created = new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(2922), new TimeSpan(0, -8, 0, 0, 0)),
                            FormControl = "",
                            FormList = "",
                            Idp = "verified",
                            Name = "Verifiable Credentials",
                            Notification = "",
                            Type = "BUTTON"
                        },
                        new
                        {
                            Id = 4,
                            Created = new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(2931), new TimeSpan(0, -8, 0, 0, 0)),
                            FormControl = "",
                            FormList = "",
                            Idp = "bcsc",
                            Name = "BC Services Card",
                            Notification = "",
                            Type = "BUTTON"
                        },
                        new
                        {
                            Id = 5,
                            Created = new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(2938), new TimeSpan(0, -8, 0, 0, 0)),
                            FormControl = "selectedAgency",
                            FormList = "filteredAgencies",
                            Idp = "submitting_agencies",
                            Name = "BCPS IDIR",
                            Notification = "",
                            Type = "AUTOCOMPLETE"
                        },
                        new
                        {
                            Id = 6,
                            Created = new DateTimeOffset(new DateTime(2024, 2, 14, 12, 53, 34, 470, DateTimeKind.Unspecified).AddTicks(2948), new TimeSpan(0, -8, 0, 0, 0)),
                            FormControl = "",
                            FormList = "",
                            Idp = "azuread",
                            Name = "BCPS Azure MFA",
                            Notification = "",
                            Type = "BUTTON"
                        });
                });

            modelBuilder.Entity("DIAMConfiguration.Data.PageContent", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Content")
                        .HasColumnType("text");

                    b.Property<string>("ContentKey")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Instant>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Instant>("Modified")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Resource")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("PageContent");
                });

            modelBuilder.Entity("DIAMConfiguration.Data.UserPreference", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("Deleted")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Preference")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.ToTable("UserPreference");
                });

            modelBuilder.Entity("HostConfigPageContent", b =>
                {
                    b.Property<int>("HostsId")
                        .HasColumnType("integer");

                    b.Property<int>("PageContentsId")
                        .HasColumnType("integer");

                    b.HasKey("HostsId", "PageContentsId");

                    b.HasIndex("PageContentsId");

                    b.ToTable("HostConfigPageContent");
                });

            modelBuilder.Entity("DIAMConfiguration.Data.LoginConfig", b =>
                {
                    b.HasOne("DIAMConfiguration.Data.HostConfig", null)
                        .WithMany("HostLoginConfigs")
                        .HasForeignKey("HostConfigId");
                });

            modelBuilder.Entity("HostConfigPageContent", b =>
                {
                    b.HasOne("DIAMConfiguration.Data.HostConfig", null)
                        .WithMany()
                        .HasForeignKey("HostsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DIAMConfiguration.Data.PageContent", null)
                        .WithMany()
                        .HasForeignKey("PageContentsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("DIAMConfiguration.Data.HostConfig", b =>
                {
                    b.Navigation("HostLoginConfigs");
                });
#pragma warning restore 612, 618
        }
    }
}
