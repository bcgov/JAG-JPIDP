﻿// <auto-generated />
using System;
using ApprovalFlow.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ApprovalFlow.Data.Migrations
{
    [DbContext(typeof(ApprovalFlowDataStoreDbContext))]
    partial class ApprovalFlowDataStoreDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("approvalflow")
                .HasAnnotation("ProductVersion", "6.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ApprovalFlow.Data.Approval.ApprovalHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Approver")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Instant>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("DecisionNote")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Instant?>("Deleted")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Instant>("Modified")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("RequestId")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("RequestId");

                    b.ToTable("ApprovalHistory", "approvalflow");
                });

            modelBuilder.Entity("ApprovalFlow.Data.Approval.ApprovalRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<Instant?>("Approved")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Instant?>("Completed")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Instant>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("IdentityProvider")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("MessageKey")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Instant>("Modified")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("ApprovalRequest", "approvalflow");
                });

            modelBuilder.Entity("ApprovalFlow.Data.Approval.Request", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ApprovalRequestId")
                        .HasColumnType("integer");

                    b.Property<string>("ApprovalType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Instant>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Instant>("Modified")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("RequestId")
                        .HasColumnType("integer");

                    b.Property<string>("RequestType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ApprovalRequestId");

                    b.ToTable("Request", "approvalflow");
                });

            modelBuilder.Entity("ApprovalFlow.Models.IdempotentConsumer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<Instant>("ConsumeDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Consumer")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("MessageId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("IdempotentConsumers", "approvalflow");
                });

            modelBuilder.Entity("ApprovalFlow.Data.Approval.ApprovalHistory", b =>
                {
                    b.HasOne("ApprovalFlow.Data.Approval.Request", "AccessRequest")
                        .WithMany("History")
                        .HasForeignKey("RequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AccessRequest");
                });

            modelBuilder.Entity("ApprovalFlow.Data.Approval.Request", b =>
                {
                    b.HasOne("ApprovalFlow.Data.Approval.ApprovalRequest", "ApprovalRequest")
                        .WithMany("Requests")
                        .HasForeignKey("ApprovalRequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ApprovalRequest");
                });

            modelBuilder.Entity("ApprovalFlow.Data.Approval.ApprovalRequest", b =>
                {
                    b.Navigation("Requests");
                });

            modelBuilder.Entity("ApprovalFlow.Data.Approval.Request", b =>
                {
                    b.Navigation("History");
                });
#pragma warning restore 612, 618
        }
    }
}
