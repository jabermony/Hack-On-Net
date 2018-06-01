﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks_Server.Database
{
    class DatabaseDump
    {
        private static List<string> commands = new List<string>
        {
            "/*!40101 SET @saved_cs_client     = @@character_set_client */",
            "/*!40101 SET character_set_client = utf8 */",
            "DROP TABLE IF EXISTS `accounts`",
            "CREATE TABLE `accounts` ("+
            " `id` int(11) NOT NULL AUTO_INCREMENT,"+
            " `username` varchar(64) NOT NULL,"+
            " `pass` char(64) DEFAULT NULL," +
            " `mailaddress` varchar(64) DEFAULT NULL,"+
            " `netmap` TEXT NOT NULL," +
            " `homeComputer` int(11) DEFAULT NULL,"+
            " `permissions` TEXT NOT NULL,"+
            " `banned` INT DEFAULT NULL,"+
            " `permBanned` BOOLEAN NOT NULL DEFAULT FALSE,"+
            " PRIMARY KEY (`id`),"+
            " UNIQUE KEY `username` (`username`),"+
            " UNIQUE KEY `mailaddress` (`mailaddress`)"+
            ") ENGINE = InnoDB AUTO_INCREMENT = 2 DEFAULT CHARSET = latin1",
            //
            // Dumping data for table `accounts`
            //
            "LOCK TABLES `accounts` WRITE",
            "/*!40000 ALTER TABLE `accounts` DISABLE KEYS */",
            "INSERT INTO `accounts` VALUES (1,'test','e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855','test@hnmp.net','',1,'admin',false,false)",
            "/*!40000 ALTER TABLE `accounts` ENABLE KEYS */",
            "UNLOCK TABLES",
            "/*!40101 SET character_set_client = @saved_cs_client */",
            "/*!40101 SET @saved_cs_client     = @@character_set_client */",
            "/*!40101 SET character_set_client = utf8 */",
            "DROP TABLE IF EXISTS `computers`",
            "CREATE TABLE `computers` (" +
            " `id` int(10) unsigned NOT NULL AUTO_INCREMENT," +
            " `ip` varchar(15) NOT NULL," +
            " `owner` varchar(64) NOT NULL," +
            " `type` int(11) NOT NULL," +
            " PRIMARY KEY (`id`)," +
            " UNIQUE KEY `ip` (`ip`)" +
            ") ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=latin1",
            //
            // Dumping data for table `computers`
            //
            "LOCK TABLES `computers` WRITE",
            "/*!40000 ALTER TABLE `computers` DISABLE KEYS */",
            "INSERT INTO `computers` VALUES (1,'8.8.8.8',1,4)",
            "/*!40000 ALTER TABLE `computers` ENABLE KEYS */",
            "UNLOCK TABLES",
            "/*!40101 SET character_set_client = @saved_cs_client */",
            "/*!40101 SET @saved_cs_client     = @@character_set_client */",
            "/*!40101 SET character_set_client = utf8 */",
            "DROP TABLE IF EXISTS `files`",
            "CREATE TABLE `files` (" +
            " `id` int(11) NOT NULL AUTO_INCREMENT," +
            " `name` varchar(255) NOT NULL," +
            " `parentFile` int(11) NOT NULL," +
            " `type` tinyint(4) NOT NULL," +
            " `content` text," +
            " `computerId` int(11) NOT NULL," +
            " `groupId` int(11) NOT NULL," +
            " `permissions` int(11) NOT NULL," +
            " `owner` int NOT NULL," +
            " PRIMARY KEY (`id`)," +
            " UNIQUE KEY `uniquefiles` (`name`,`parentFile`,`computerId`)" +
            ") ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=latin1",
            "/*!40101 SET character_set_client = @saved_cs_client */",
            //
            // Dumping data for table `files`
            //
            "LOCK TABLES `files` WRITE",
            "/*!40000 ALTER TABLE `files` DISABLE KEYS */",
            "INSERT INTO `files` VALUES " +
            "(1,'',0,1,'',1,0,774,0)," +
            "(2,'daemons',1,0,'',1,1,774,0)," +
            "(3,'autorun',2,0,'irc\r\nbank',1,1,774,0)," +
            "(4,'irc',2,0,'IRC',1,0,774,0)," +
            "(5,'etc',1,1,'',1,1,774,0)," +
            "(6,'passwd',5,0,'" +
            "root:x:0:0:root:/root:/bin/hash\r\n" +
            "admin:x:1:1:root:/root:/bin/hash\r\n" +
            "user:x:2:2:root:/root:/bin/hash\r\n" +
            "guest:x:3:3:root:/root:/bin/hash\r\n" +
            "',1,1,774,0),"+
            "(7,'group',5,0,'" +
            "root:x:0:\r\n" +
            "admin:x:1:root,admin\r\n" +
            "user:x:2:root,admin,user\r\n" +
            "guest:x:3:root,admin,user,guest\r\n" +
            "',1,1,774,0),"+
            "(8,'bank',2,0,'BANK',1,0,774,0)," +
            "(9,'bank',1,1,'bank',1,0,774,0)," +
            "(10,'accounts.db',9,0,'',1,0,774,0)," +
            "(11,'bin',1,1,'',1,0,774,0)," +
            "(0,'hackybox',11,0,'hackybox',1,0,774,0),"+
            "(0,'ping',11,0,'hackybox',1,0,774,0),"+
            "(0,'connect',11,0,'hackybox',1,0,774,0),"+
            "(0,'disconnect',11,0,'hackybox',1,0,774,0),"+
            "(0,'dc',11,0,'hackybox',1,0,774,0),"+
            "(0,'ls',11,0,'hackybox',1,0,774,0),"+
            "(0,'touch',11,0,'hackybox',1,0,774,0),"+
            "(0,'view',11,0,'hackybox',1,0,774,0),"+
            "(0,'mkdir',11,0,'hackybox',1,0,774,0),"+
            "(0,'rm',11,0,'hackybox',1,0,774,0),"+
            "(0,'login',11,0,'hackybox',1,0,774,0),"+
            "(0,'chown',11,0,'hackybox',1,0,774,0),"+
            "(0,'chmod',11,0,'hackybox',1,0,774,0),"+
            "(0,'fedit',11,0,'hackybox',1,0,774,0),"+
            "(0,'netmap',11,0,'hackybox',1,0,774,0),"+
            "(0,'music',11,0,'hackybox',1,0,774,0),"+
            "(0,'admin',11,0,'serveradmin',1,0,774,0),"+
            "(0,'cadmin',11,0,'computeradmin',1,0,774,0),"+
            "(0,'hash',11,0,'hash',1,0,774,0)",
            "/*!40000 ALTER TABLE `files` ENABLE KEYS */",

            "UNLOCK TABLES",
            "DROP TABLE IF EXISTS `binaries`",
            "CREATE TABLE `binaries` (" +
            " `id` int(11) PRIMARY KEY NOT NULL AUTO_INCREMENT," +
            " `checksum` int NOT NULL," +
            " `type` char(64) NOT NULL" +
            ") ENGINE = InnoDB AUTO_INCREMENT = 1 DEFAULT CHARSET = latin1",
            //
            // Dumping data for table `binaries`
            //
            $"INSERT INTO `binaries` VALUES "+
            $"(0,{"hackybox".GetHashCode()},'Hackybox'),"+
            $"(0,{"serveradmin".GetHashCode()},'ServerAdmin'),"+
            $"(0,{"computeradmin".GetHashCode()},'ComputerAdmin')",
        };

        public static List<string> Commands => commands;
    }
}
