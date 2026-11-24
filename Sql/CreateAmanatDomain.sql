IF DB_ID(N'AmanatDomain') IS NULL
BEGIN
    CREATE DATABASE [AmanatDomain];
END;
GO
USE [AmanatDomain];
GO
:r Sql/001_CreateDomainSchema.sql
GO
