﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using edt.casemanagement.Data;

#nullable disable

namespace edt.casemanagement.Migrations
{
    [DbContext(typeof(CaseManagementDataStoreDbContext))]
    [Migration("20230308215707_RequestErrorDetails")]
    partial class RequestErrorDetails
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("casemgmt")
                .HasAnnotation("ProductVersion", "6.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("edt.casemanagement.Models.CaseRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AgencFileNumber")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("CaseId")
                        .HasColumnType("integer");

                    b.Property<Instant>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Instant>("Modified")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("PartyId")
                        .HasColumnType("integer");

                    b.Property<bool>("RemoveRequested")
                        .HasColumnType("boolean");

                    b.Property<Instant?>("Requested")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("CaseRequest", "casemgmt");
                });
#pragma warning restore 612, 618
        }
    }
}
