﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using edt.service.Data;

#nullable disable

namespace edt.service.Data.Migrations
{
    [DbContext(typeof(EdtDataStoreDbContext))]
    [Migration("20220912000625_EdtService_InitialCreate")]
    partial class EdtService_InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("edt")
                .HasAnnotation("ProductVersion", "6.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("edt.service.ServiceEvents.UserAccountCreation.Models.EmailLog", b =>
                {
                    b.Property<int>("CaseId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("CaseId"), 1L, 1);

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Cc")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DateSent")
                        .HasColumnType("datetime2");

                    b.Property<string>("LatestStatus")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("Modified")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("MsgId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("SendType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SentTo")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StatusMessage")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UpdateCount")
                        .HasColumnType("int");

                    b.HasKey("CaseId");

                    b.ToTable("EmailLog", "edt");
                });

            modelBuilder.Entity("edt.service.ServiceEvents.UserAccountCreation.Models.IdempotentConsumer", b =>
                {
                    b.Property<string>("MessageId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Consumer")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("MessageId", "Consumer");

                    b.ToTable("IdempotentConsumers", "edt");
                });

            modelBuilder.Entity("edt.service.ServiceEvents.UserAccountCreation.Models.NotificationAckModel", b =>
                {
                    b.Property<string>("NotificationId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("EmailAddress")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Consumer")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("PartId")
                        .HasColumnType("bigint");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("NotificationId", "EmailAddress");

                    b.ToTable("Notifications", "edt");
                });
#pragma warning restore 612, 618
        }
    }
}
