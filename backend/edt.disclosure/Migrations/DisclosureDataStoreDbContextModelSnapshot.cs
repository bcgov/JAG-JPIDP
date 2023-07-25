﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using edt.disclosure.Data;

#nullable disable

namespace edt.disclosure.Migrations
{
    [DbContext(typeof(DisclosureDataStoreDbContext))]
    partial class DisclosureDataStoreDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("disclosure")
                .HasAnnotation("ProductVersion", "6.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("edt.disclosure.ServiceEvents.Models.CourtLocationRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("CaseId")
                        .HasColumnType("integer");

                    b.Property<Instant>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Instant?>("DeletedOn")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Location")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Instant>("Modified")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("PartyId")
                        .HasColumnType("integer");

                    b.Property<bool>("RemoveRequested")
                        .HasColumnType("boolean");

                    b.Property<Instant?>("Requested")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("SubLocation")
                        .HasColumnType("text");

                    b.Property<string>("status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("CourtLocationRequest", "disclosure");
                });

            modelBuilder.Entity("edt.disclosure.ServiceEvents.UserAccountCreation.Models.IdempotentConsumer", b =>
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

                    b.ToTable("IdempotentConsumers", "disclosure");
                });
#pragma warning restore 612, 618
        }
    }
}
