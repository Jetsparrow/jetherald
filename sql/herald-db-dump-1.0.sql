CREATE DATABASE  IF NOT EXISTS `herald` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `herald`;
-- MySQL dump 10.13  Distrib 8.0.28, for Win64 (x86_64)
--
-- Host: localhost    Database: herald
-- ------------------------------------------------------
-- Server version	8.0.26

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `heart`
--

DROP TABLE IF EXISTS `heart`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `heart` (
  `HeartId` bigint unsigned NOT NULL AUTO_INCREMENT,
  `TopicId` int unsigned NOT NULL,
  `Heart` varchar(32) NOT NULL,
  `Status` enum('beating','stopped','deleted') NOT NULL DEFAULT 'beating',
  `LastBeatTs` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ExpiryTs` timestamp NOT NULL,
  `CreateTs` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateTs` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`HeartId`),
  UNIQUE KEY `idx_TopicId_Heart_UNIQUE` (`TopicId`,`Heart`),
  CONSTRAINT `fk_topic` FOREIGN KEY (`TopicId`) REFERENCES `topic` (`TopicId`)
) ENGINE=InnoDB AUTO_INCREMENT=115140 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `heartevent`
--

DROP TABLE IF EXISTS `heartevent`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `heartevent` (
  `HeartEventId` bigint unsigned NOT NULL AUTO_INCREMENT,
  `TopicId` int unsigned NOT NULL,
  `Heart` varchar(32) NOT NULL,
  `Status` enum('created','processing','reported') NOT NULL DEFAULT 'created',
  `Event` enum('created','started','stopped','deleted') NOT NULL COMMENT 'ENUM(''created'',''started'',''stopped'',''deleted'')',
  `CreateTs` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`HeartEventId`),
  KEY `idx_topic` (`TopicId`,`CreateTs`),
  KEY `idx_reported` (`Status`)
) ENGINE=InnoDB AUTO_INCREMENT=201 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `topic`
--

DROP TABLE IF EXISTS `topic`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `topic` (
  `TopicId` int unsigned NOT NULL AUTO_INCREMENT,
  `Creator` varchar(45) NOT NULL,
  `Name` varchar(45) NOT NULL,
  `Description` varchar(45) NOT NULL,
  `ReadToken` char(32) NOT NULL,
  `WriteToken` char(32) NOT NULL,
  `AdminToken` char(32) NOT NULL,
  `CreateTs` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateTs` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`TopicId`),
  UNIQUE KEY `TopicName_UNIQUE` (`Name`),
  UNIQUE KEY `ReadToken_UNIQUE` (`ReadToken`),
  KEY `Creator_INDEX` (`Creator`)
) ENGINE=InnoDB AUTO_INCREMENT=54 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `topic_sub`
--

DROP TABLE IF EXISTS `topic_sub`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `topic_sub` (
  `TopicId` int unsigned NOT NULL,
  `Sub` varchar(45) NOT NULL,
  `CreateTs` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`TopicId`,`Sub`),
  CONSTRAINT `FK_TOPICID` FOREIGN KEY (`TopicId`) REFERENCES `topic` (`TopicId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2022-01-28 16:35:26
